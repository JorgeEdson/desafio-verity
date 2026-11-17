# ADR-003 — Transactional Outbox para publicação confiável

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

Registrar um lançamento envolve duas escritas em sistemas diferentes: o banco (o lançamento) e o broker (o evento). Sem transação distribuída, temos o problema do **dual write**:

- grava no banco e o publish falha → o consolidado nunca fica sabendo do lançamento (**saldo errado, silenciosamente**);
- publica e o commit falha → o consolidado conta um lançamento que não existe.

Além disso, publicar direto no broker durante a requisição tornaria Lançamentos dependente da disponibilidade do RabbitMQ, o que contraria o espírito do RNF01.

## Decisão

**Transactional Outbox**, reaproveitando o desenho já validado no projeto *simple-erp*:

1. Os eventos de domínio levantados pelo agregado são capturados por um **interceptor do EF Core** e gravados na tabela `outbox` **no mesmo `SaveChanges`** do lançamento.
2. Um **`BackgroundService` (Outbox Relay)** dentro da Lançamentos API lê os pendentes em lotes (`FOR UPDATE SKIP LOCKED`, seguro com várias réplicas), publica no RabbitMQ com *publisher confirm* e marca `publicado_em_utc`. Se uma publicação falha, o lote é interrompido e retomado no ciclo seguinte; não há limite de tentativas, porque a falha típica é o broker indisponível, não uma mensagem defeituosa.
3. Intervalo de 1 s quando ocioso; sem pausa quando o lote vem cheio, para drenar acúmulos rapidamente.
4. Falhas incrementam `tentativas` e registram `ultimo_erro`; o evento continua pendente.


## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Publicar direto no broker após o commit | Dual write: uma queda entre o commit e o publish perde o evento. |
| Transação distribuída (2PC) | Não suportada pelo RabbitMQ, e indesejável por acoplar a disponibilidade dos dois. |
| Outbox de um framework (MassTransit, Wolverine) | Funciona, mas traz a dependência inteira por um recurso que custa cerca de 150 linhas. |

## Consequências

**Positivas**
- Atomicidade entre o fato de negócio e a sua notificação.
- **Lançamentos funciona com o RabbitMQ fora:** os eventos se acumulam no outbox e saem quando o broker volta.

**Negativas**
- Entrega **at-least-once**: uma queda entre o publish e o `UPDATE publicado_em_utc` republica o evento. Tratado em [ADR-004](ADR-004-consumo-idempotente.md).
- Atraso adicional de até cerca de 1 s (o intervalo do relay).
- A tabela cresce: os eventos publicados devem ser expurgados periodicamente (ex.: após 7 dias).
