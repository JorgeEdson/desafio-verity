# 6. Estratégia de testes

Os testes servem a dois propósitos: provar que o **domínio** está correto e provar que os **requisitos não funcionais** são atendidos. O segundo grupo é o que diferencia esta solução: um RNF que não é testado é só uma afirmação.

## 6.1 Pirâmide

| Nível | O que cobre | Ferramentas | Exemplos |
|---|---|---|---|
| **Unitário** (base, maioria) | Entidades, objetos de valor, use cases com dependências substituídas | xUnit, NSubstitute, FluentAssertions | `Valor` rejeita zero e negativo; estorno inverte o tipo; um estorno não pode ser estornado; data futura é rejeitada |
| **Integração** | Repositórios, migrations, outbox, consumer, consultas SQL reais | xUnit + **Testcontainers** (PostgreSQL, RabbitMQ) | Lançamento e outbox gravados atomicamente; consumer idempotente; propagação retroativa |
| **API** | Pipeline HTTP completo: autenticação, policy, serialização, Problem Details | `WebApplicationFactory` com emissor de token de teste | `401` sem token; `404` ao acessar o lançamento de outro comerciante |
| **Resiliência** | RNF01 e RNF09 | Testcontainers (parar e iniciar containers) | Ver 6.2 |
| **Carga** | RNF02–RNF04 | k6 | Ver 6.3 |

> Nos testes de API, o token é gerado por um emissor de teste com uma chave RSA gerada em memória. Isso evita depender do Keycloak na suíte automatizada sem enfraquecer a validação, porque é o mesmo pipeline `JwtBearer` de produção.

## 6.2 Cenários de resiliência

| # | Cenário | Resultado esperado |
|---|---|---|
| R1 | Parar RabbitMQ e Consolidado; registrar 100 lançamentos | 100 × `201`; 100 eventos pendentes no outbox |
| R2 | Religar o RabbitMQ (continuação de R1) | Outbox drenado; 100 mensagens na fila |
| R3 | Religar o Worker (continuação de R2) | Saldo consolidado = soma dos 100 lançamentos |
| R4 | Entregar o mesmo evento 3 vezes | Saldo afetado uma única vez |
| R5 | Entregar os eventos dos dias 10, 12 e 11 nessa ordem | Saldo de fechamento correto nos três dias |
| R6 | Lançamento retroativo no dia 5 com dias 6–15 já consolidados | Dia 5 e dias 6–15 ajustados pelo delta; dias anteriores intactos |
| R7 | Dois Workers processando em paralelo eventos do mesmo comerciante para um dia novo | Nenhum saldo divergente (valida o lock consultivo) |
| R8 | Mensagem com payload inválido | Vai para a DLQ após o limite de entregas; o restante da fila segue sendo processado |
| R9 | Mesmo `POST /lancamentos` repetido 3 vezes com a mesma `Idempotency-Key` | 1 × `201` e 2 × `200`; um único lançamento e um único evento no outbox |
| R10 | 10 requisições **simultâneas** com a mesma `Idempotency-Key` | Um único lançamento persistido; todas as respostas devolvem o mesmo id |
| R11 | Mesma `Idempotency-Key` com valor diferente | `422`; nenhum lançamento novo |
| R12 | `POST /lancamentos` sem `Idempotency-Key` | `400` |

## 6.3 Teste de carga (k6)

`tests/carga/consolidado-50rps.js`:

- Executor `constant-arrival-rate`: **50 iterações por segundo durante 2 minutos**, independente da latência (é isso que simula carga de pico de verdade, ao contrário de um número fixo de usuários virtuais).
- Pré-condição: um comerciante de teste com 90 dias de lançamentos, criados pelo próprio script na etapa `setup`, pela API de Lançamentos.
- Cada iteração consulta um dia aleatório dentro desses 90 dias, para não medir só o cache.
- *Thresholds*, que fazem o k6 falhar se não forem atingidos:
  - `http_req_failed: rate < 0.05` (enunciado) e uma segunda execução com `rate < 0.01` (meta interna);
  - `http_req_duration: p(95) < 200, p(99) < 500`.
- **Variação com falha:** aos 60 s, `docker compose stop` de uma das réplicas da Consolidado API. O resultado esperado é continuar dentro dos *thresholds*, graças ao health check ativo do gateway.

O k6 roda em container na rede do compose (`docker compose --profile carga run --rm k6 run consolidado-50rps.js`), sem instalação local. O resultado de cada execução (resumo do k6) fica versionado em `tests/carga/resultados/` como evidência. Instruções completas em [`tests/carga/README.md`](../tests/carga/README.md).

## 6.4 Cobertura

A meta é cobertura alta no `Core` (≥ 80%), onde moram as regras. A infraestrutura é coberta pelos testes de integração, sem meta numérica, porque um percentual de cobertura em adaptadores mede pouco.
