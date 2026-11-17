# 5. Implantação: local e produção

**Princípio:** o código das aplicações é **o mesmo** nos dois ambientes. Mudam apenas a configuração (variáveis de ambiente) e a infraestrutura. "Pronto para produção" aqui significa que cada componente tem sua versão de produção definida e justificada, e não que adicionamos componentes extras.

## 5.1 Ambiente local (`docker compose`)

| Serviço | Imagem | Observações |
|---|---|---|
| gateway | build de `src/Gateway` | Único ponto exposto para as APIs |
| lancamentos-api | build | Aplica as migrations na inicialização (apenas em dev) |
| consolidado-api | build, **2 réplicas** | Demonstra o balanceamento e a tolerância à queda de uma réplica |
| consolidado-worker | build | |
| postgres | `postgres:17` | **Um** container com **dois databases** e usuários distintos, por economia local. Nenhum serviço acessa o banco do outro. |
| rabbitmq | `rabbitmq:4-management` | Exchanges, filas e DLQ declaradas via `definitions.json` |
| keycloak | `quay.io/keycloak/keycloak` | Realm `verity` importado de `infra/keycloak/realm-verity.json`, com usuários de teste |
| k6 | `grafana/k6:2.0.0` | Só com `--profile carga`: executa o teste de carga de [`tests/carga`](../tests/carga/README.md) na rede do compose |
| aspire-dashboard | `mcr.microsoft.com/dotnet/aspire-dashboard:13.0` | Recebe OTLP de todos os serviços; interface em http://localhost:18888 (acesso anônimo, só local) |

Todos os serviços .NET usam o **mesmo `Dockerfile`**, na raiz do repositório, parametrizado pelos argumentos de build `PROJETO` (caminho do `.csproj`) e `ASSEMBLY` (nome da dll de entrada). A imagem final é a `aspnet:10.0`, executa com usuário sem privilégios (`$APP_UID`) e o restore usa cache de pacotes do BuildKit ([ADR-016](adr/ADR-016-blocos-de-construcao-compartilhados.md)).

Os comandos de execução e as credenciais de teste ficam no `README.md` da raiz do repositório.

## 5.2 Ambiente de produção (referência)

A referência usa Azure. A tabela traz equivalentes AWS, porque a arquitetura **não depende** de provedor: depende de Postgres, AMQP, OIDC e OTLP.

| Componente | Local | Produção | Equivalente AWS |
|---|---|---|---|
| Borda | — | Application Gateway + WAF, TLS | ALB + AWS WAF |
| Gateway YARP | 1 réplica | 2+ réplicas em zonas diferentes | — |
| Lançamentos API | 1 réplica | 2+ réplicas; autoscaling por CPU/RPS | ECS/EKS |
| Consolidado API | 2 réplicas | 2+ réplicas; autoscaling por RPS | ECS/EKS |
| Consolidado Worker | 1 réplica | 2+ réplicas; autoscaling pela profundidade da fila (KEDA) | ECS/EKS + KEDA |
| PostgreSQL | 1 container, 2 databases | **2 servidores** gerenciados, HA com réplica síncrona em outra zona e backup com PITR | RDS/Aurora Multi-AZ |
| RabbitMQ | 1 nó | Cluster de 3 nós com quorum queues, ou serviço gerenciado | Amazon MQ for RabbitMQ |
| IdP | Keycloak `start-dev` | Keycloak `start` com 2+ instâncias, banco próprio, TLS e realm versionado como código; **ou** Entra ID | Cognito |
| Segredos | `.env` | Key Vault, com identidade gerenciada | Secrets Manager |
| Observabilidade | Aspire Dashboard | OTel Collector → Azure Monitor / Grafana, com alertas de SLO | CloudWatch / AMP |
| Migrations | Na inicialização da API | **Etapa separada no pipeline**, com usuário próprio, antes do deploy | — |

### Keycloak em produção

Hospedar o Keycloak você mesmo significa **operar um sistema de identidade**: atualizações de segurança frequentes, backup do banco dele, monitoramento e plantão. Quando a organização não quer esse custo, a troca para um IdP gerenciado é só configuração (`Authority`, `Audience` e o mapeamento da claim `comerciante_id`), sem mudança de código. Ver [ADR-008](adr/ADR-008-autenticacao-keycloak.md).

## 5.3 Pipeline (CI/CD)

1. **Build e testes** a cada PR: unitários e integração com Testcontainers.
2. **Análise:** `dotnet format --verify-no-changes`, análise de vulnerabilidades em pacotes (`dotnet list package --vulnerable`) e scan das imagens.
3. **Imagens** versionadas pelo SHA do commit.
4. **Migrations** aplicadas como etapa própria. As migrations devem ser compatíveis com a versão anterior do código (*expand/contract*), o que permite rolling deploy sem downtime.
5. **Deploy rolling** com *readiness probe* (`/health/ready`) e rollback automático se os health checks falharem.
6. **Teste de fumaça** e, em pré-produção, o script k6 de 50 req/s como *gate*.

## 5.4 Continuidade de negócio

| Item | Estratégia |
|---|---|
| Backup | PITR gerenciado nos dois Postgres (RPO ≤ 5 min) |
| Restore | Exercício trimestral de restore em ambiente isolado (RTO ≤ 30 min) |
| Perda do consolidado_db | **Reconstruível**: é um modelo derivado. Basta republicar os eventos a partir dos lançamentos (ver [Evoluções](07-evolucoes-futuras.md), "Reconstrução do consolidado") |
| Perda do lancamentos_db | É a fonte da verdade: depende do backup/PITR e da réplica HA |
