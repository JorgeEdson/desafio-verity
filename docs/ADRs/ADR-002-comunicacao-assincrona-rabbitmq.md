# ADR-002 — Comunicação assíncrona entre os contextos via RabbitMQ

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

O Consolidado precisa saber de cada lançamento registrado, sem que Lançamentos dependa da disponibilidade do Consolidado ([ADR-001](ADR-001-estilo-arquitetural.md)). Precisamos de um meio que **guarde** as mensagens enquanto o consumidor estiver fora e as entregue quando ele voltar.

Características do fluxo: um produtor, um consumidor, um tipo de evento, ordem irrelevante (deltas aditivos, ver [ADR-005](ADR-005-modelo-do-consolidado.md)) e volume baixo.

## Decisão

**RabbitMQ**, com:
- uma exchange `lancamentos` (topic) e uma **quorum queue** `consolidado.lancamentos`;
- mensagens *persistent* e *publisher confirms* no produtor;
- `ack` manual no consumidor, **depois** do commit;
- `x-delivery-limit` (ex.: 5) com *dead-letter* para `consolidado.lancamentos.dlq`;
- cliente oficial `RabbitMQ.Client` (API assíncrona), encapsulado atrás de interfaces na `Infraestrutura`.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **HTTP síncrono** de Lançamentos para Consolidado | Viola diretamente o RNF de disponibilidade. |
| **HTTP com retry e circuit breaker** | Para não perder o evento com o Consolidado fora, precisaria persistir as chamadas pendentes, o que reinventa uma fila. |
| **Kafka** | Log particionado e reprocessável, para vazões muito maiores. Não precisamos de *replay* nem de ordem por partição, e o custo operacional é maior. Ver [Evoluções](../07-evolucoes-futuras.md). |
| **Consolidado fazendo polling do outbox de Lançamentos** | Remove o broker, mas acopla o Consolidado ao banco de Lançamentos ([ADR-013](ADR-013-banco-de-dados.md)). |
| **MassTransit** sobre o RabbitMQ | Ótima abstração (retry, outbox, sagas), mas a partir da v9 passou a ter licenciamento comercial, e usamos uma fração mínima dos recursos. O código direto com `RabbitMQ.Client` fica pequeno e explícito. |
| **Azure Service Bus / SQS** | Boas opções gerenciadas em produção, mas sem emulação local equivalente e simples para o avaliador. A abstração na `Infraestrutura` permite trocar depois. |

## Consequências

**Positivas**
- O Consolidado pode ficar fora pelo tempo que for: as mensagens aguardam na fila durável.
- A DLQ isola mensagens problemáticas sem travar a fila.
- O retry vem do próprio broker, via quorum queue com `delivery-limit`, sem código próprio de retry.

**Negativas**
- **Consistência eventual:** o consolidado fica alguns segundos atrás (meta no [RNF06](../03-requisitos-nao-funcionais.md)).
- Mais um componente para operar. Em produção: cluster de 3 nós ou serviço gerenciado.
- A entrega é *at-least-once*, o que exige idempotência ([ADR-004](ADR-004-consumo-idempotente.md)).
