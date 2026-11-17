# Verity — Fluxo de Caixa

Solução para o desafio de arquitetura de software da Verity: um comerciante registra **lançamentos** (débitos e créditos) e consulta o **saldo diário consolidado**.

A solução tem dois serviços independentes em .NET 10, cada um com o próprio banco PostgreSQL, que se comunicam **somente por eventos** via RabbitMQ. Por isso o serviço de **Lançamentos continua disponível mesmo com o Consolidado fora do ar**. O Consolidado mantém o saldo de cada dia já calculado e atende o pico de **50 requisições por segundo com no máximo 5% de perda**. Os dois ficam atrás de um gateway YARP e são autenticados pelo Keycloak (OIDC/JWT).

> Documentação completa (requisitos, segurança, implantação, estratégia de testes): [`docs/`](docs/README.md).

## Arquitetura

### Nível 1 — Contexto

![Diagrama de contexto](docs/diagramas/Nivel%201%20Contexto.png)

### Nível 2 — Containers

![Diagrama de containers](docs/diagramas/Nivel%202%20Containers.png)

## Decisões arquiteturais

| ADR | Decisão |
|---|---|
| [001](docs/ADRs/ADR-001-estilo-arquitetural.md) | Dois contextos independentes (Lançamentos e Consolidado), com banco próprio e **nenhuma chamada síncrona** entre eles. |
| [002](docs/ADRs/ADR-002-comunicacao-assincrona-rabbitmq.md) | Integração por eventos no **RabbitMQ**: quorum queue, *publisher confirms*, `ack` após o commit e DLQ. |
| [003](docs/ADRs/ADR-003-transactional-outbox.md) | **Transactional Outbox**: o evento é gravado na mesma transação do lançamento e publicado por um relay em segundo plano. |
| [004](docs/ADRs/ADR-004-consumo-idempotente.md) | Entrega *at-least-once* com **consumo idempotente** (`evento_processado`) e lock por comerciante. |
| [005](docs/ADRs/ADR-005-modelo-do-consolidado.md) | **Saldo de fechamento materializado** por dia, atualizado incrementalmente e propagado para frente em lançamentos retroativos. |
| [006](docs/ADRs/ADR-006-lancamentos-imutaveis.md) | Lançamentos **imutáveis**: a correção é feita por estorno, nunca por edição ou exclusão. |
| [007](docs/ADRs/ADR-007-multi-comerciante.md) | Isolamento por **`comerciante_id` vindo do token**, com filtro global no EF Core. |
| [008](docs/ADRs/ADR-008-autenticacao-keycloak.md) | **Keycloak** como provedor de identidade OIDC; cada API valida o JWT (RS256). |
| [009](docs/ADRs/ADR-009-gateway-yarp.md) | **Gateway fino com YARP**: roteamento, balanceamento e health check, sem regra de negócio nem validação de token. |
| [010](docs/ADRs/ADR-010-cache.md) | **Cache de saída** em memória por comerciante, com TTL de 5 s, na API do Consolidado. |
| [011](docs/ADRs/ADR-011-estrutura-interna-dos-servicos.md) | Estrutura **Api / Core / Infraestrutura** em cada serviço, com dispatcher próprio no lugar do MediatR. |
| [012](docs/ADRs/ADR-012-observabilidade.md) | **OpenTelemetry** (traces, métricas e logs), com um trace único do registro à consolidação. |
| [013](docs/ADRs/ADR-013-banco-de-dados.md) | **PostgreSQL com um banco por serviço**; um serviço nunca acessa o banco do outro. |
| [014](docs/ADRs/ADR-014-idempotencia-na-api.md) | **`Idempotency-Key` obrigatória** nos comandos: um retry nunca gera lançamento duplicado. |
| [015](docs/ADRs/ADR-015-pacote-compartilhado-de-seguranca.md) | **Segurança das APIs num pacote compartilhado**, para uma única implementação da validação de token. |
| [016](docs/ADRs/ADR-016-blocos-de-construcao-compartilhados.md) | **Blocos técnicos compartilhados** (hospedagem, infraestrutura, testes) e um **Dockerfile único**. |
| [017](docs/ADRs/ADR-017-limite-de-requisicoes-por-comerciante.md) | **Limite de requisições por comerciante** (*token bucket*) nas APIs, bem acima da carga nominal. |

## Pré-requisitos

- **Docker Desktop** com Docker Compose v2, para rodar o ambiente, os testes de integração e o teste de carga.
- **.NET SDK 10**, só para rodar os testes automatizados.
- **Insomnia 9.3 ou superior**, para o teste de ponta a ponta.
- Portas livres: `8080` (gateway), `8081` (Keycloak), `5432` (PostgreSQL), `5672` e `15672` (RabbitMQ), `18888` e `4317` (Aspire Dashboard).

## Como rodar localmente

Na raiz do repositório:

```powershell
docker compose up -d --build
docker compose ps
```

Espere todos os serviços ficarem `running` ou `healthy`; o Keycloak leva cerca de 30 segundos para importar o realm. As migrations são aplicadas automaticamente na subida.

| Serviço | Endereço | Acesso |
|---|---|---|
| Gateway (entrada das APIs) | http://localhost:8080 | Token do Keycloak |
| Keycloak | http://localhost:8081 | Console: `admin` / `admin` |
| RabbitMQ (console) | http://localhost:15672 | `admin` / `admin` |
| Aspire Dashboard (traces, métricas e logs) | http://localhost:18888 | Sem login |

Usuários de teste: **`loja.centro`** (comerciante `c-001`) e **`loja.praia`** (comerciante `c-002`), ambos com a senha **`Verity@2026!`**.

## Testes

### 1. Testes automatizados

```powershell
dotnet test Verity.slnx
```

Testes unitários e de integração. Os de integração sobem PostgreSQL e RabbitMQ descartáveis via Testcontainers, por isso o Docker precisa estar em execução.

### 2. Teste de ponta a ponta (Insomnia)

Com o ambiente no ar:

1. No Insomnia, **Import** → [`tests/insomnia/verity.insomnia.json`](tests/insomnia/verity.insomnia.json).
2. Execute as pastas **na ordem**, pelo **Collection Runner**:

| Pasta | O que comprova |
|---|---|
| 01 · Preparação e saúde | Gateway e Keycloak no ar; gera as datas e o identificador da execução |
| 02 · Autenticação | Tokens das duas lojas, com a claim `comerciante_id` correta |
| 03 · Segurança nas APIs | Sem token ou com token adulterado → `401` |
| 04 · Linha de base | Guarda o saldo antes dos lançamentos |
| 05 · Lançamentos | Crédito, débito e lançamento retroativo; consulta e extrato |
| 06 · Idempotência e estorno | Repetição sem duplicar (`200`), chave reaproveitada (`422`), estorno e estorno duplicado (`409`) |
| 07 · Validações | Data futura, valor zero, três casas decimais, tipo inválido, período acima de 93 dias |
| 08 · Isolamento | Uma loja não lê nem estorna lançamentos da outra (`404`) |
| 09 · Consolidado | Saldo do dia, propagação do retroativo e período sem lacunas |

3. **Resiliência (pasta 10, manual).** Rode antes as pastas 01 e 02. Depois envie uma requisição por vez, intercalando com os comandos no terminal:

| Passo | Envie no Insomnia | Depois, no terminal |
|---|---|---|
| 1 | Saldo de hoje antes da queda | `docker compose stop consolidado-api-1` |
| 2 | Uma réplica fora → `200` | `docker compose stop consolidado-api-2 consolidado-worker` |
| 3 | Consolidado inteiro fora → **`201`** (lançamentos continuam disponíveis) | — |
| 4 | Consulta ao consolidado → `502`/`503` | `docker compose start consolidado-api-1 consolidado-api-2 consolidado-worker` e aguarde ~10 s |
| 5 | Após a retomada → saldo = base + 500,00 (nada se perdeu) | — |

Detalhes em [`tests/insomnia/README.md`](tests/insomnia/README.md).

### 3. Teste de carga (k6)

O k6 roda em container, na rede do compose, sem instalação local. O script cria 90 dias de lançamentos e aplica **50 req/s por 2 minutos** no Consolidado, falhando se a perda passar de 5% ou se o p95 passar de 200 ms.

```powershell
docker compose --profile carga run --rm k6 run consolidado-50rps.js
```

Meta interna (perda abaixo de 1%):

```powershell
docker compose --profile carga run --rm -e TAXA_DE_ERRO_MAXIMA=0.01 -e CENARIO=consolidado-50rps-meta-interna k6 run consolidado-50rps.js
```

Com queda de uma réplica durante a carga:

```powershell
docker compose --profile carga run --rm -e CENARIO=consolidado-50rps-com-falha k6 run consolidado-50rps.js
```

Cerca de 60 segundos depois de a carga começar, em outro terminal, rode `docker compose stop consolidado-api-1`; ao final, `docker compose start consolidado-api-1`.

O resumo de cada execução é salvo em [`tests/carga/resultados/`](tests/carga/resultados/). Durante a carga, o Aspire Dashboard mostra a latência e as métricas em tempo real. Detalhes em [`tests/carga/README.md`](tests/carga/README.md).
