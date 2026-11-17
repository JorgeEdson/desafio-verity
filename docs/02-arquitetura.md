# 2. Arquitetura da solução

Este documento descreve a solução em níveis de zoom crescentes, no estilo do [modelo C4](https://c4model.com/): contexto, containers, componentes e, por fim, os fluxos e o modelo de dados. As justificativas de cada escolha estão nos [ADRs](adr/README.md); aqui está o **o quê** e o **como**.

## 2.1 Visão em uma frase

Dois serviços independentes, **Lançamentos** (escrita) e **Consolidado** (leitura), cada um com seu banco, ligados **apenas por eventos assíncronos** entregues de forma confiável (outbox + RabbitMQ). Ficam atrás de um gateway fino e são protegidos por tokens emitidos por um IdP OpenID Connect.

## 2.2 Nível 1 — Contexto

![Diagrama de contexto](diagramas/01-contexto.png)

> Fonte PlantUML: [`diagramas/01-contexto.puml`](diagramas/01-contexto.puml)

## 2.3 Nível 2 — Containers

![Diagrama de containers](diagramas/02-containers.png)

> Fonte PlantUML: [`diagramas/02-containers.puml`](diagramas/02-containers.puml)

| Container | Responsabilidade | Tecnologia | Escala |
|---|---|---|---|
| **Gateway** | Ponto único de entrada; roteia por prefixo; balanceia entre réplicas; health check ativo dos destinos. **Não** valida token nem contém regra de negócio. | YARP (.NET) | Stateless, N réplicas |
| **Lançamentos API** | Registra, estorna e consulta lançamentos. Grava o evento no outbox na mesma transação. Hospeda o *Outbox Relay*, que publica os eventos pendentes. | ASP.NET Core | Stateless, N réplicas (o relay usa `FOR UPDATE SKIP LOCKED`, seguro com várias réplicas) |
| **lancamentos_db** | Fonte da verdade dos lançamentos e do outbox | PostgreSQL | Vertical + réplica de leitura em produção |
| **RabbitMQ** | Desacopla temporalmente os dois contextos; absorve picos e indisponibilidades do Consolidado | RabbitMQ (quorum queue) | Cluster de 3 nós em produção |
| **Consolidado Worker** | Consome `LancamentoRegistrado` e mantém a tabela `saldo_diario` | .NET Worker Service | Competing consumers |
| **Consolidado API** | Serve o relatório de saldo diário e de período | ASP.NET Core + OutputCache | Stateless, N réplicas — **é o container sujeito aos 50 req/s** |
| **consolidado_db** | Modelo de leitura (saldo de fechamento materializado) e controle de idempotência | PostgreSQL | Vertical + réplica de leitura |
| **Keycloak** | Emite tokens JWT (RS256) com a claim `comerciante_id` | Keycloak | 2+ instâncias em produção, ou IdP gerenciado |

### Por que a API e o Worker do Consolidado são processos separados

Eles pertencem ao **mesmo contexto** e compartilham `Core` e `Infraestrutura`, mas rodam em hosts diferentes:

- O RNF de 50 req/s recai sobre a **leitura**. Separar impede que um backlog de consolidação (por exemplo, depois de o Consolidado ficar fora do ar) dispute CPU e conexões com as consultas.
- Cada parte escala de forma independente: réplicas da API pela demanda de leitura, réplicas do Worker pela profundidade da fila.

## 2.4 Nível 3 — Componentes

Cada serviço segue a mesma organização interna, herdada do projeto *simple-erp* (ver [ADR-011](adr/ADR-011-estrutura-interna-dos-servicos.md)):

![Diagrama de componentes](diagramas/03-componentes.png)

> Fonte PlantUML: [`diagramas/03-componentes.puml`](diagramas/03-componentes.puml)

Regra de dependência: `Api → Core ← Infraestrutura`. O `Core` não referencia EF, RabbitMQ nem ASP.NET.

### Projetos da solução

```
src/
├── Compartilhado/
│   ├── Verity.Compartilhado                 # primitivas técnicas (Resultado, Entidade, use cases, dispatcher)
│   │                                        # + contratos de eventos de integração. Sem regra de negócio.
│   ├── Verity.Compartilhado.Seguranca       # autenticação JWT, policy RequerComerciante, IComercianteAtual (ADR-015)
│   ├── Verity.Compartilhado.Hospedagem      # controller base, Problem Details, health checks live/ready (ADR-016)
│   ├── Verity.Compartilhado.Infraestrutura  # PostgreSQL + EF, migrations, design-time, conexão RabbitMQ (ADR-016)
│   └── Verity.Compartilhado.Observabilidade # OpenTelemetry: traces, métricas e logs via OTLP (ADR-012)
├── Gateway/
│   └── Verity.Gateway                  # YARP
├── Lancamentos/
│   ├── Verity.Lancamentos.Api
│   ├── Verity.Lancamentos.Core
│   └── Verity.Lancamentos.Infraestrutura
└── Consolidado/
    ├── Verity.Consolidado.Api
    ├── Verity.Consolidado.Worker
    ├── Verity.Consolidado.Core
    └── Verity.Consolidado.Infraestrutura
tests/
├── Verity.Testes.Compartilhado              # emissor de tokens, factory autenticada, containers (ADR-016)
├── Verity.Gateway.Testes
├── Verity.Lancamentos.Testes
├── Verity.Consolidado.Testes
└── carga/                              # scripts k6
Dockerfile                                   # único, parametrizado por PROJETO e ASSEMBLY (ADR-016)
infra/
├── keycloak/realm-verity.json
├── rabbitmq/definitions.json
└── postgres/init.sql
```

## 2.5 Contratos de API

Todas as rotas passam pelo gateway e exigem `Authorization: Bearer <token>`. O `ComercianteId` **nunca** vem da rota nem do corpo: é sempre extraído da claim `comerciante_id` do token. Os erros seguem *Problem Details* ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)).

### Lançamentos

| Método e rota | Descrição | Respostas |
|---|---|---|
| `POST /lancamentos` | Registra um lançamento. **Header `Idempotency-Key` obrigatório** ([ADR-014](adr/ADR-014-idempotencia-na-api.md) · [ADR-015](adr/ADR-015-pacote-compartilhado-de-seguranca.md)). | `201` + `Location`; `200` se a chave já foi usada com o mesmo conteúdo (devolve o lançamento original); `422` se a chave já foi usada com conteúdo diferente; `400` se faltar a chave; `401` |
| `POST /lancamentos/{id}/estornos` | Estorna um lançamento (RN03, RN04). **Header `Idempotency-Key` obrigatório.** | `201`; `200` na repetição com a mesma chave; `404`; `409` se já estornado por outra requisição ou se for um estorno |
| `GET /lancamentos/{id}` | Detalhe de um lançamento | `200`; `404` |
| `GET /lancamentos?inicio=&fim=&pagina=&tamanhoDaPagina=` | Extrato paginado por data de competência (período de até 93 dias, até 100 itens por página) | `200`; `400` |

```json
POST /lancamentos
Idempotency-Key: 7d0f3a52-9b8e-4c1a-a6f2-2f1e9c3b5d10

{
  "tipo": "Credito",
  "valor": 8000.00,
  "dataCompetencia": "2026-11-15",
  "descricao": "Recebimento de salário"
}
```

### Consolidado

| Método e rota | Descrição | Respostas |
|---|---|---|
| `GET /consolidado/diario/{data}` | Saldo consolidado de um dia | `200` (sempre: um dia sem movimento devolve movimento zero e o saldo vigente) |
| `GET /consolidado/diario?inicio=&fim=` | Série diária de um período (máx. 93 dias), com os dias sem movimento preenchidos | `200`; `400` |

```json
GET /consolidado/diario/2026-11-15
{
  "data": "2026-11-15",
  "saldoInicial": 5000.00,
  "totalCreditos": 8500.00,
  "totalDebitos": 3200.00,
  "saldoFinal": 10300.00,
  "atualizadoEmUtc": "2026-11-15T18:42:07Z"
}
```

O campo `atualizadoEmUtc` explicita a consistência eventual: o cliente sabe a partir de quando aquela fotografia é válida.

## 2.6 Contrato do evento de integração

Há **um único** evento de integração. O estorno também o emite, com o tipo invertido, porque para o Consolidado ele é só mais um movimento.

```json
{
  "eventoId": "0193a8f2-6c1e-7b4a-9d0e-1a2b3c4d5e6f",
  "versao": 1,
  "ocorridoEmUtc": "2026-11-16T14:03:22Z",
  "lancamentoId": "0193a8f2-6c1d-7f10-8e21-9b8c7d6e5f40",
  "comercianteId": "c-001",
  "tipo": "Debito",
  "valor": 1200.00,
  "dataCompetencia": "2026-11-15"
}
```

- `eventoId` é a chave de idempotência do consumidor.
- `versao` permite evoluir o contrato sem quebrar consumidores antigos (mudanças aditivas mantêm a versão; mudanças incompatíveis criam a v2 e as duas convivem durante a transição).
- O tipo do evento vai na propriedade `type` da mensagem AMQP e o `traceparent` do OpenTelemetry vai nos **headers**, não no corpo.

## 2.7 Fluxos principais

### 2.7.1 Registrar lançamento — o caminho crítico de disponibilidade

1. O gateway encaminha `POST /lancamentos` à Lançamentos API, que valida o token e aplica o limite de requisições do comerciante.
2. O use case valida a entrada e verifica a `Idempotency-Key`: se a chave já existe com o mesmo conteúdo, devolve o lançamento original (`200`); com conteúdo diferente, `422`.
3. Numa **única transação**, grava o lançamento e, pelo interceptor do EF Core, o evento `LancamentoRegistrado` no outbox, com o `traceparent` da requisição.
4. Responde `201` com o lançamento e o header `Location`.

A chave de idempotência garante que um retry do cliente (timeout, queda de rede, duplo clique) nunca gere um segundo lançamento ([ADR-014](adr/ADR-014-idempotencia-na-api.md) · [ADR-015](adr/ADR-015-pacote-compartilhado-de-seguranca.md)).

**Observe o que não aparece aqui:** RabbitMQ e Consolidado. O registro de um lançamento depende apenas da própria API e do próprio banco. É isso que atende ao RNF de disponibilidade.

### 2.7.2 Publicação confiável (Outbox Relay)

1. A cada ciclo (1 s), o relay abre uma transação e lê até 50 eventos pendentes com `FOR UPDATE SKIP LOCKED`, o que permite várias réplicas sem publicar o mesmo evento em paralelo.
2. Publica cada evento no exchange `verity.lancamentos` com *publisher confirms* e `mandatory`, levando o `traceparent` no header da mensagem.
3. Marca `publicado_em_utc` e confirma a transação. Na primeira falha, interrompe o lote; os restantes ficam pendentes para o próximo ciclo.

Garantia resultante: **at-least-once**. Uma queda entre o `publish` e o `UPDATE` causa republicação, e é por isso que o consumidor é idempotente.

### 2.7.3 Consolidação (inclui lançamento retroativo)

1. O Worker recebe a mensagem da quorum queue `consolidado.lancamentos` e abre um span ligado ao `traceparent` do header.
2. Numa transação: `pg_advisory_xact_lock` do comerciante, `INSERT` em `evento_processado` (se o evento já existe, é duplicado e encerra), criação do dia a partir do último saldo anterior, se ainda não existir, `UPDATE` do dia e `UPDATE` de todos os dias posteriores (propagação do retroativo).
3. `ack` somente depois do `COMMIT`.

Pontos de projeto:

- **A ordem das mensagens não importa.** Cada evento aplica um delta aditivo (`+valor` ou `−valor`), e somas são comutativas. Por isso não precisamos de fila ordenada nem de particionamento por comerciante.
- **O lock consultivo por comerciante** serializa apenas eventos do **mesmo** comerciante. Ele evita a corrida em que dois eventos criam a linha de um dia novo calculando o saldo base ao mesmo tempo. Comerciantes diferentes são processados em paralelo.
- **Tudo numa transação:** registro de idempotência, atualização do dia e propagação para frente. Ou tudo acontece, ou nada.
- **Falha permanente vs. transitória.** Uma mensagem ilegível ou que viola o contrato (versão não suportada, tipo desconhecido, valor inválido) é rejeitada **sem reenfileirar** e vai direto para a DLQ: tentar de novo não mudaria o resultado. Uma falha transitória (banco indisponível, timeout) é repetida **dentro do próprio Worker**, com espera exponencial de 1 s até 30 s e sem limite de tentativas. A mensagem fica sem `ack` até o banco voltar, então uma queda prolongada do banco não descarta eventos na DLQ.
- **Rede de segurança:** se o Worker cair no meio do processamento, a mensagem volta para a fila. Depois de *N* entregas (`x-delivery-limit` da quorum queue), ela vai para a DLQ e gera alerta, sem bloquear o resto da fila.
- **Custo da propagação:** um único `UPDATE` sobre os dias posteriores de um comerciante, que são no máximo algumas centenas de linhas por ano, usando o índice da PK.

### 2.7.4 Consulta do saldo diário

1. O gateway balanceia `GET /consolidado/diario/{data}` entre as réplicas saudáveis da Consolidado API.
2. A API valida o token, aplica o limite de requisições e procura a resposta no cache de saída (chave: comerciante e parâmetros; TTL de 5 s).
3. Sem cache, busca o último dia com movimento até a data pedida (uma consulta pela PK) e calcula o dia: se não houve movimento na data, herda o saldo final anterior (RN07).

Uma consulta por índice, sem agregação em tempo de leitura: é isso que torna os 50 req/s triviais.

## 2.8 Modelo de dados

### lancamentos_db

| Tabela | Colunas |
|---|---|
| `lancamentos` | `id` (UUID v7, PK), `comerciante_id`, `tipo`, `valor`, `data_competencia`, `descricao`, `estorno_de_id`, `chave_de_idempotencia`, `hash_da_requisicao`, `registrado_em_utc` |
| `outbox` | `id` (identidade, PK), `evento_id`, `tipo_do_evento`, `chave_de_roteamento`, `conteudo` (jsonb), `contexto_de_rastreamento`, `ocorrido_em_utc`, `publicado_em_utc`, `tentativas`, `ultimo_erro` |

- Índice único `(comerciante_id, chave_de_idempotencia)`: a garantia final contra duplicidade, inclusive com requisições concorrentes.
- `hash_da_requisicao` (SHA-256 do corpo normalizado) detecta reuso da mesma chave com conteúdo diferente.
- Índice `(comerciante_id, data_competencia)` para o extrato.
- Índice parcial `outbox(id) WHERE publicado_em_utc IS NULL` para o relay.
- Índice único parcial `lancamentos(estorno_de_id) WHERE estorno_de_id IS NOT NULL`, que garante a RN04 no banco e não só no código.

### consolidado_db

| Tabela | Colunas |
|---|---|
| `saldo_diario` | `comerciante_id` + `data` (PK), `total_creditos`, `total_debitos`, `saldo_final`, `atualizado_em_utc` |
| `evento_processado` | `evento_id` (PK), `processado_em_utc` |

- `saldo_inicial` não é armazenado: é derivado como `saldo_final − total_creditos + total_debitos`.
- Só existem linhas para dias **com movimento**. Os dias sem movimento são resolvidos na leitura (RN07), o que evita criar 365 linhas por comerciante por ano sem necessidade.
- `evento_processado` pode ser expurgada após um período bem maior que a janela máxima de redelivery (por exemplo, 30 dias).

## 2.9 Decisões relacionadas

[ADR-001](adr/ADR-001-estilo-arquitetural.md) · [ADR-002](adr/ADR-002-comunicacao-assincrona-rabbitmq.md) · [ADR-003](adr/ADR-003-transactional-outbox.md) · [ADR-004](adr/ADR-004-consumo-idempotente.md) · [ADR-005](adr/ADR-005-modelo-do-consolidado.md) · [ADR-006](adr/ADR-006-lancamentos-imutaveis.md) · [ADR-007](adr/ADR-007-multi-comerciante.md) · [ADR-008](adr/ADR-008-autenticacao-keycloak.md) · [ADR-009](adr/ADR-009-gateway-yarp.md) · [ADR-010](adr/ADR-010-cache.md) · [ADR-011](adr/ADR-011-estrutura-interna-dos-servicos.md) · [ADR-012](adr/ADR-012-observabilidade.md) · [ADR-013](adr/ADR-013-banco-de-dados.md) · [ADR-014](adr/ADR-014-idempotencia-na-api.md) · [ADR-015](adr/ADR-015-pacote-compartilhado-de-seguranca.md) · [ADR-016](adr/ADR-016-blocos-de-construcao-compartilhados.md) · [ADR-017](adr/ADR-017-limite-de-requisicoes-por-comerciante.md)
