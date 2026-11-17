# ADR-005 — Saldo de fechamento materializado, com propagação retroativa

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

O saldo diário consolidado é o **saldo de fechamento** do dia: saldo do dia anterior + créditos do dia − débitos do dia ([Visão geral](../01-visao-geral.md)). Duas decisões de negócio afetam o modelo:

- **lançamentos retroativos são permitidos** (RN05), então alterar o dia D muda o fechamento de D e de todos os dias seguintes;
- o Consolidado precisa responder a **50 req/s** com no máximo 5% de perda.

## Decisão

Materializar, no consolidado_db, a tabela `saldo_diario (comerciante_id, data, total_creditos, total_debitos, saldo_final)`, mantida **incrementalmente** pelo Worker:

1. Para um evento com delta `±v` e competência `D`:
   - se não existe linha para `D`, ela é criada com `saldo_final` = fechamento do último dia anterior a `D` (ou 0);
   - os totais e o `saldo_final` de `D` são atualizados com `±v`;
   - **propagação:** `UPDATE saldo_diario SET saldo_final = saldo_final ± v WHERE comerciante_id = :c AND data > :D`.
2. Tudo em uma transação, junto com o controle de idempotência ([ADR-004](ADR-004-consumo-idempotente.md)).
3. Só existem linhas para dias com movimento. A leitura de um dia D busca a última linha com `data <= D`: se for o próprio D, devolve a linha; senão, movimento zero e saldo igual ao `saldo_final` encontrado.
4. `saldo_inicial` é derivado (`saldo_final − créditos + débitos`), não armazenado.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **Calcular na leitura** (`SUM` de todos os lançamentos até D) | O custo cresce com o histórico e cai sobre o caminho dos 50 req/s; além disso, exigiria acesso aos lançamentos. |
| **Guardar só o movimento do dia** e somar os dias na leitura (`SUM(...) OVER`) | Simples na escrita, mas a leitura soma até 365+ linhas por consulta. Viável na escala atual, porém coloca o custo justamente onde está o requisito. |
| **Proibir retroativos** | Simplificaria a escrita, mas esquecer de lançar algo é comum em fluxo de caixa real. A propagação custa um `UPDATE`. |
| **Snapshots mensais + movimento diário** | Otimização para históricos muito longos; prematura. |

## Consequências

**Positivas**
- A leitura é **uma consulta indexada pela PK**, independente do tamanho do histórico.
- Retroativos e estornos são tratados pelo mesmo mecanismo: só mais um delta.
- Como os deltas são aditivos, a ordem de processamento é irrelevante.

**Negativas**
- Um retroativo antigo toca muitas linhas: no máximo cerca de 365 por ano de histórico, num único `UPDATE` indexado, o que é aceitável. Se surgir uma regra de **fechamento de período** ([Evoluções](../07-evolucoes-futuras.md)), o alcance é limitado naturalmente.
- **Dias passados não são imutáveis.** Isso invalida estratégias de cache longo para "dias fechados" ([ADR-010](ADR-010-cache.md)).
- O modelo é derivado: um bug na consolidação corrompe o saldo. Mitigação: testes R4–R7 e, como evolução, o job de reconciliação e a reconstrução a partir dos eventos.
