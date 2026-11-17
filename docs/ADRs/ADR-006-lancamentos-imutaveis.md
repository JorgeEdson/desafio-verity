# ADR-006 — Lançamentos imutáveis, com correção por estorno

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

Registros financeiros precisam de trilha de auditoria. Além disso, cada tipo de alteração em um lançamento (editar valor, editar data, excluir) exigiria um evento próprio e uma lógica de compensação própria no Consolidado.

## Decisão

Lançamentos são **imutáveis**: sem `PUT`, `PATCH` nem `DELETE`. A correção é feita por **estorno**: um novo lançamento de tipo inverso, mesmo valor e mesma data de competência, com `estorno_de_id` apontando para o original.

- Um lançamento só pode ser estornado uma vez, e um estorno não pode ser estornado. Isso é garantido também por um **índice único parcial** no banco.
- Para corrigir um valor errado: estorna-se o original e registra-se um novo lançamento correto.
- O estorno emite o mesmo `LancamentoRegistrado`; para o Consolidado, é só mais um delta.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| CRUD completo | Perde o histórico; exige eventos `LancamentoAlterado`/`LancamentoExcluido` com valores antigos e novos, e compensação no Consolidado. |
| Exclusão lógica (*soft delete*) | Mantém o registro, mas continua exigindo um evento de compensação e confunde as consultas. |
| Event sourcing | Daria a auditoria, mas com custo de complexidade não justificado ([Evoluções](../07-evolucoes-futuras.md)). |

## Consequências

**Positivas**
- Trilha de auditoria completa, sem mecanismo adicional.
- **Um único tipo de evento de integração**, o que simplifica o contrato, o consumidor e os testes.
- Prática alinhada à contabilidade (não se apaga um lançamento, faz-se um estorno).

**Negativas**
- Corrigir um erro exige duas operações (estornar e relançar) e polui o extrato. Mitigação: o extrato pode ocultar pares estornados, como opção de visualização.
