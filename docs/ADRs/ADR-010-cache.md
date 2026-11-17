# ADR-010 — Cache de saída em memória com TTL curto

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

O enunciado pede que se considerem estratégias de cache. A leitura do consolidado já é uma consulta pela PK ([ADR-005](ADR-005-modelo-do-consolidado.md)), suficiente para 50 req/s. O cache aqui é **proteção contra rajadas** (o mesmo comerciante consultando o mesmo dia repetidamente), e não uma necessidade de desempenho.

Uma ideia inicial era cachear "dias fechados" (anteriores a hoje) por muito tempo, por serem imutáveis. **Essa premissa caiu** quando decidimos permitir lançamentos retroativos: qualquer dia passado pode mudar.

## Decisão

- **OutputCache** do ASP.NET Core em cada réplica da Consolidado API.
- Chave: `comerciante_id` (da claim) + rota + parâmetros. **Nunca** sem o comerciante na chave, ou um comerciante veria os dados de outro.
- **TTL de 5 s, igual para todos os dias.** Isso entra no orçamento do atraso de consistência ([RNF06](../03-requisitos-nao-funcionais.md)).
- Sem invalidação ativa: o TTL curto torna desnecessária a coordenação entre o Worker e as réplicas.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **Sem cache** | Atenderia os 50 req/s, mas deixaria o banco sem proteção contra rajadas e ignoraria um item explícito do enunciado. |
| **TTL longo para dias passados** | Incorreto com retroativos permitidos. |
| **Redis com invalidação pelo Worker** | Daria um cache consistente e compartilhado, mas adiciona um componente, um ponto de falha e a lógica de invalidação, para um ganho marginal nesta escala ([Evoluções](../07-evolucoes-futuras.md)). |
| **Cache HTTP no cliente** (`Cache-Control`, `ETag`) | Complementar, e pode ser adicionado depois; não protege o servidor de clientes que não o respeitam. |

## Consequências

**Positivas**
- Custo de implementação quase zero.
- O banco fica protegido contra leituras repetidas.

**Negativas**
- Cada réplica tem seu próprio cache: a taxa de acerto cai à medida que o número de réplicas cresce. Irrelevante na escala atual.
- Até 5 s de atraso adicional na visão do consolidado.
