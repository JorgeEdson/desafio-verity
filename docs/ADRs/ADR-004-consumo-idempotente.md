# ADR-004 — Entrega at-least-once com consumo idempotente

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

O outbox ([ADR-003](ADR-003-transactional-outbox.md)) pode republicar, e o RabbitMQ pode reentregar mensagens (queda do consumidor antes do `ack`, *redelivery*). *Exactly-once* de ponta a ponta não existe na prática. Como o consolidado **soma valores**, processar um evento duas vezes significa **saldo errado**.

## Decisão

**At-least-once na entrega, efeito exactly-once no consumidor**, por meio de:

1. Uma tabela `evento_processado (evento_id PK)` no consolidado_db.
2. Na **mesma transação** da atualização do saldo: `INSERT ... ON CONFLICT DO NOTHING`. Se nenhuma linha for inserida, o evento é duplicado: rollback e `ack`.
3. `ack` **somente depois** do `COMMIT`. Se o processo cair antes do `ack`, a reentrega é descartada pela regra 2.
4. **Lock consultivo por comerciante** (`pg_advisory_xact_lock`) no início da transação, que serializa eventos do mesmo comerciante e elimina a corrida na criação do registro de um dia novo. Comerciantes diferentes seguem em paralelo.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Confiar que não haverá duplicatas | Elas acontecem, e o erro seria silencioso num sistema financeiro. |
| Deduplicação no broker | O RabbitMQ não oferece deduplicação nativa confiável para este caso. |
| Recalcular o dia inteiro a partir dos lançamentos | Exigiria que o Consolidado lesse o banco de Lançamentos ([ADR-013](ADR-013-banco-de-dados.md)). |
| Fila com consumidor único (*single active consumer*) para garantir a serialização | Funciona, mas limita a vazão a um consumidor para **todos** os comerciantes. O lock por comerciante dá a mesma garantia, com paralelismo entre comerciantes. |

## Consequências

**Positivas**
- O saldo é correto com duplicatas, reentregas e eventos fora de ordem (os deltas são aditivos).
- Permite várias instâncias do Worker (*competing consumers*).

**Negativas**
- Mais uma tabela, que precisa de expurgo (ex.: registros com mais de 30 dias, janela muito maior que qualquer reentrega).
- O lock serializa eventos de um mesmo comerciante. Na escala do problema, isso é irrelevante.
