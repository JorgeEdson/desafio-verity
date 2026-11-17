# ADR-009 — Gateway fino com YARP

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

Funcionalmente, um gateway **não é necessário**: são só duas APIs, sem frontend, e cada uma consegue validar tokens e limitar requisições sozinha. Porém:

- o enunciado pede explicitamente escalabilidade horizontal **com balanceamento de carga**;
- o RNF de 50 req/s fica mais robusto, e **demonstrável**, com 2+ réplicas da Consolidado API e alguém distribuindo a carga e removendo réplicas doentes;
- os clientes ganham um ponto único de entrada (um host, um certificado, CORS centralizado).

## Decisão

Um gateway com **YARP** (biblioteca de reverse proxy da Microsoft, em C#), com responsabilidades **estritamente** limitadas a:

- roteamento por prefixo (`/lancamentos/*`, `/consolidado/*`);
- balanceamento entre réplicas (*PowerOfTwoChoices*, o padrão do YARP);
- health check **ativo** dos destinos (`/health/ready`), que remove réplicas doentes;
- limite de tamanho de payload e propagação de `traceparent`.

O gateway **não** valida tokens, **não** aplica regras de negócio e **não** agrega respostas. Cada API continua validando o JWT (defesa em profundidade: o gateway não é uma fronteira de confiança).

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **Sem gateway** | Defensável. Perderíamos a demonstração prática do balanceamento e da tolerância à queda de uma réplica, que são pedidos do enunciado. |
| **Nginx / Traefik** | Funcionam bem, mas fogem do requisito "feito em C#" e exigem outra linguagem de configuração. |
| **Ocelot** | Projeto com manutenção menos ativa que o YARP. |
| **Gateway "gordo"** (auth, rate limit, agregação) | Concentra responsabilidades e lógica num ponto único de falha; vira um monolito na borda. |
| **API Management gerenciado** | É a opção de produção para a borda externa (com WAF), mas não roda localmente. |

## Consequências

**Positivas**
- Demonstra escalabilidade horizontal e failover de réplica (validado pelo k6 com uma réplica derrubada).
- Código mínimo: um `Program.cs` e as rotas no `appsettings.json`.

**Negativas**
- **Ponto único de falha.** Se o gateway cair, os dois serviços ficam inacessíveis ao cliente, mesmo saudáveis. O isolamento do RNF01 continua valendo entre os serviços, mas não na borda. Mitigação em produção: 2+ réplicas em zonas diferentes, atrás de um load balancer gerenciado.
- Mais um salto de rede (latência adicional pequena, da ordem de 1 ms).
- **Risco de virar depósito de lógica.** Regra explícita: nada além do listado acima entra no gateway sem um novo ADR.
- Rate limiting na borda geraria `429`, que conta como perda no RNF03. Por isso o rate limiting é por comerciante, nas APIs, e com limite alto.
