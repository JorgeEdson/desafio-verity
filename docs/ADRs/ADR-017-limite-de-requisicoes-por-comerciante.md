# ADR-017 — Limite de requisições por comerciante

- **Status:** Aceito
- **Data:** 2026-11-17

## Contexto

As APIs são multi-comerciante. Sem limite, um único comerciante (por erro de integração, laço de retry ou abuso) consegue consumir a capacidade compartilhada e degradar os demais. O [ADR-009](ADR-009-gateway-yarp.md) já decidiu que esse controle fica **nas APIs**, depois da autenticação, porque depende da claim `comerciante_id`, e que o gateway não valida token.

Há uma restrição do enunciado: respostas `429` contam como perda no RNF03 (no máximo 5% sob 50 req/s no Consolidado). O limite, portanto, não pode morder a carga nominal.

## Decisão

- **Algoritmo:** *token bucket* por comerciante (`System.Threading.RateLimiting`, nativo do ASP.NET Core). Permite rajadas curtas acima da taxa média sem rejeitar, e repõe a capacidade de forma contínua.
- **Partição:** o `comerciante_id` do token, lido por `IComercianteAtual`. Requisições sem comerciante (verificações de saúde, ou anônimas, que a autorização já recusa) não são limitadas.
- **Posição no pipeline:** depois de `UseAuthentication`/`UseAuthorization` e antes do cache de saída. Uma requisição atendida pelo cache também conta, porque o objetivo é limitar o cliente, não o banco.
- **Resposta:** `429 Too Many Requests` em Problem Details, com o código estável `LIMITE_DE_REQUISICOES_EXCEDIDO` em `erros` e o header `Retry-After`. Sem fila de espera (`QueueLimit = 0`): esperar no servidor só aumentaria a latência e prenderia conexões.
- **Implementação compartilhada:** `AdicionarLimiteDeRequisicoesPorComerciante` em `Verity.Compartilhado.Hospedagem`, configurada pela seção `LimiteDeRequisicoesPorComerciante` de cada API ([ADR-016](ADR-016-blocos-de-construcao-compartilhados.md)).

| API | Rajada | Reposição | Por quê |
|---|---|---|---|
| Consolidado | 200 | 100 req/s | 2× a carga de pico **total** do enunciado (50 req/s), aplicada **por comerciante** e **por réplica**: a carga nominal nunca é limitada. |
| Lançamentos | 100 | 50 req/s | Escrita custa mais (transação, outbox, índice de idempotência); 50 lançamentos por segundo por comerciante já é muito acima de qualquer operação de caixa real. |

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Limitar no gateway | O gateway não conhece o comerciante (não valida token). Um limite global por IP puniria todos os comerciantes atrás do mesmo NAT e geraria `429` contando como perda. |
| Janela fixa | Permite o dobro do limite na virada da janela e rejeita rajadas legítimas no início dela. |
| Limite distribuído (Redis) | Seria exato entre réplicas, mas adiciona uma dependência no caminho de toda requisição. Com limites folgados, a imprecisão do limite local (que cresce com o número de réplicas) é aceitável. |
| Fila de espera no servidor | Converte excesso em latência e conexões presas; sob abuso, piora o serviço para todos. |

## Consequências

**Positivas**
- Um comerciante não degrada os demais; a carga do enunciado nunca é rejeitada.
- O cliente sabe quando tentar de novo (`Retry-After`) e pode tratar o erro pelo código.
- As métricas do ASP.NET Core (`aspnetcore.rate_limiting.*`) já chegam ao dashboard pelo OpenTelemetry.

**Negativas**
- O limite é **por réplica**: com N réplicas, o teto efetivo de um comerciante é até N vezes o configurado. Se for preciso um teto exato, a evolução é um limitador distribuído ([Evoluções futuras](../07-evolucoes-futuras.md)).
- Ataques volumétricos sem autenticação não são cobertos aqui; ficam com o WAF na borda.
