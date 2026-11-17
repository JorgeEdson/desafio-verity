# 3. Requisitos não funcionais

Requisito não funcional sem número não é requisito, é desejo. Cada item abaixo tem uma **meta**, o **mecanismo** que a atende e a **forma de verificação**.

## 3.1 Metas

| ID | Atributo | Meta | Origem |
|---|---|---|---|
| RNF01 | Disponibilidade isolada | Lançamentos continua aceitando registros com o Consolidado, o Worker **ou o RabbitMQ** fora do ar | Enunciado |
| RNF02 | Vazão do Consolidado | 50 req/s sustentados no pico | Enunciado |
| RNF03 | Perda no Consolidado | Taxa de erro ≤ 5% sob 50 req/s. **Meta interna: < 1%** | Enunciado (meta interna mais rígida) |
| RNF04 | Latência do Consolidado | p95 < 200 ms e p99 < 500 ms sob 50 req/s | Definida pelo arquiteto |
| RNF05 | Latência de Lançamentos | p95 < 300 ms no `POST /lancamentos` | Definida pelo arquiteto |
| RNF06 | Atraso de consistência | p95 < 5 s entre o `201` do lançamento e o reflexo no consolidado, com o sistema saudável | Definida pelo arquiteto |
| RNF07 | Durabilidade | Nenhum lançamento confirmado com `201` é perdido; nenhum evento é perdido entre os contextos | Integridade financeira |
| RNF08 | Exatidão | O saldo consolidado é igual à soma dos lançamentos, mesmo com mensagens duplicadas, fora de ordem ou retroativas | Integridade financeira |
| RNF09 | Recuperação | Após a volta do Consolidado, o backlog é drenado automaticamente, sem intervenção manual | Resiliência |
| RNF10 | SLO de produção | Lançamentos 99,9%/mês; Consolidado 99,5%/mês | Definida pelo arquiteto |
| RNF11 | Continuidade (produção) | RPO ≤ 5 min; RTO ≤ 30 min | Definida pelo arquiteto |

## 3.2 Como cada meta é atendida

### RNF01 — Disponibilidade isolada

| Falha | Comportamento | Mecanismo |
|---|---|---|
| Consolidado API fora | Lançamentos não percebe nada | Não existe chamada de Lançamentos para Consolidado ([ADR-001](adr/ADR-001-estilo-arquitetural.md)) |
| Consolidado Worker fora | As mensagens se acumulam na fila durável; são processadas quando ele voltar | Quorum queue persistente ([ADR-002](adr/ADR-002-comunicacao-assincrona-rabbitmq.md)) |
| RabbitMQ fora | Os eventos se acumulam no outbox, no próprio banco de Lançamentos; o relay republica quando o broker voltar | Transactional outbox ([ADR-003](adr/ADR-003-transactional-outbox.md)) |
| consolidado_db fora | Igual ao Worker fora | Idem |
| Keycloak fora | Tokens já emitidos continuam válidos até expirar (as chaves públicas ficam em cache nas APIs); novos logins falham | JWKS em cache ([ADR-008](adr/ADR-008-autenticacao-keycloak.md)) |
| Gateway fora | **Tudo fica inacessível ao cliente.** Mitigado em produção com 2+ réplicas atrás de um load balancer gerenciado | [ADR-009](adr/ADR-009-gateway-yarp.md), risco aceito e documentado |

Os únicos pontos que derrubam o registro de lançamentos são a própria Lançamentos API, o lancamentos_db e a borda (gateway e IdP). Isso é o mínimo possível.

### RNF02 a RNF04 — Vazão, perda e latência do Consolidado

50 req/s é uma carga baixa para ASP.NET Core. O risco não está no volume, e sim em **operações caras por requisição** e em **pontos únicos**. Mecanismos:

1. **Leitura O(1):** o saldo de fechamento é materializado na escrita; a leitura é uma consulta pela PK, sem `SUM` em tempo de leitura ([ADR-005](adr/ADR-005-modelo-do-consolidado.md)).
2. **Cache de saída** por comerciante + data, com TTL curto, que absorve rajadas de leituras repetidas ([ADR-010](adr/ADR-010-cache.md)).
3. **2 réplicas** da Consolidado API atrás do gateway, com health check ativo: a queda de uma réplica não derruba o serviço ([ADR-009](adr/ADR-009-gateway-yarp.md)).
4. **Leitura e consolidação em processos separados:** um backlog no Worker não rouba recursos das consultas.
5. **Pool de conexões dimensionado** e timeouts explícitos no acesso ao banco: falhar rápido é melhor que enfileirar requisições.

**O que conta como perda:** qualquer resposta `5xx`, timeout ou erro de conexão, além de respostas `429` caso um rate limit seja ativado. Por isso o rate limiting fica configurado bem acima da carga nominal: 100 req/s por comerciante e por réplica no Consolidado, o dobro do pico total do enunciado ([ADR-017](adr/ADR-017-limite-de-requisicoes-por-comerciante.md)).

### RNF06 — Atraso de consistência

| Componente | Contribuição típica |
|---|---|
| Intervalo do outbox relay | ≤ 1 s (sem pausa quando o lote vem cheio) |
| Publicação e entrega no RabbitMQ | dezenas de ms |
| Transação de consolidação | dezenas de ms |
| TTL do cache de leitura | ≤ 5 s |

Pior caso saudável: cerca de 6 s; caso típico: cerca de 1 a 2 s. O campo `atualizadoEmUtc` da resposta torna esse atraso visível ao cliente.

### RNF07 e RNF08 — Durabilidade e exatidão

- `Idempotency-Key` obrigatório, com índice único: um retry do cliente nunca cria um segundo lançamento ([ADR-014](adr/ADR-014-idempotencia-na-api.md)).
- O lançamento e o evento são gravados **na mesma transação** (outbox).
- As mensagens são publicadas como *persistent* e com *publisher confirms*. O consumidor só dá `ack` **depois** do `COMMIT`.
- A idempotência por `evento_id`, gravada na mesma transação da atualização do saldo, neutraliza duplicatas.
- Os deltas são aditivos, então a ordem de chegada é irrelevante.
- O lock consultivo por comerciante elimina a condição de corrida na criação de dias novos.
- **Verificação contínua (evolução):** um job de reconciliação compara periodicamente a soma dos lançamentos com o saldo consolidado ([Evoluções](07-evolucoes-futuras.md)).

### RNF09 — Recuperação automática

Não há passo manual: o relay e o Worker são laços contínuos com retry. Mensagens que falham repetidamente vão para a DLQ, e só elas exigem análise humana (com alerta).

## 3.3 Verificação

| RNF | Como é verificado | Onde |
|---|---|---|
| RNF01 | Teste de integração que derruba o RabbitMQ e o Consolidado (Testcontainers) e registra lançamentos com sucesso | `tests/Verity.Lancamentos.Testes` |
| RNF02–04 | Script k6 com taxa constante de 50 req/s por 2 min e *thresholds* `http_req_failed < 0.05` (meta interna `< 0.01`) e `p(95) < 200` | `tests/carga/consolidado-50rps.js` |
| RNF02–03 sob falha | Mesmo script, derrubando uma das réplicas da Consolidado API no meio da execução | Roteiro em [Estratégia de testes](06-estrategia-de-testes.md) |
| RNF06 | Teste de integração que mede o tempo entre o `POST` e o reflexo no `GET` | `tests/Verity.Consolidado.Testes` |
| RNF07–08 | Testes que publicam o mesmo evento duas vezes, eventos fora de ordem e lançamentos retroativos, e conferem o saldo | `tests/Verity.Consolidado.Testes` |
| RNF10–11 | Monitoramento em produção (SLIs no OpenTelemetry) e exercícios periódicos de restore | [Implantação](05-implantacao.md) |

## 3.4 Métricas operacionais

Os indicadores pedidos no enunciado, ligados a sinais concretos:

| Dimensão | SLI | Alerta sugerido |
|---|---|---|
| **Disponibilidade** | % de requisições não-5xx por serviço | Consumo do *error budget* acima de 2× o ritmo em 1 h |
| **Confiabilidade** | Taxa de erro e latência p95/p99 por rota | p95 > 200 ms por 5 min no Consolidado |
| **Integridade** | Divergência entre a soma dos lançamentos e o saldo (job de reconciliação); mensagens na DLQ | Qualquer divergência; DLQ > 0 |
| **Atraso de consistência** | Eventos pendentes no outbox; profundidade da fila; idade da mensagem mais antiga | Outbox pendente > 1.000 ou mensagem mais antiga > 60 s |

As métricas da aplicação que alimentam esses SLIs estão listadas no [ADR-012](adr/ADR-012-observabilidade.md#implementação): `verity.outbox.eventos_pendentes`, `verity.outbox.idade_do_evento_pendente_mais_antigo`, `verity.consolidacao.eventos_processados` (rejeitados vão para a DLQ), `verity.consolidacao.atraso_desde_o_registro` e as métricas HTTP por rota do ASP.NET Core.
