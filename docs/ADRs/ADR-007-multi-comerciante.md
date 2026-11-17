# ADR-007 — Isolamento por `comerciante_id` vindo do token

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

O enunciado fala de "um comerciante", mas uma solução real atende vários. Adicionar o identificador depois exigiria migração de dados e mudança em todas as consultas e eventos. O risco principal em APIs multi-cliente é o acesso a dados de outro cliente (BOLA, OWASP API1).

## Decisão

- Todas as tabelas de negócio, o evento de integração e a chave do saldo carregam `comerciante_id`.
- O identificador vem **exclusivamente** da claim `comerciante_id` do JWT, exposta à aplicação por `IComercianteAtual`. Nunca é aceito na rota, na query ou no corpo.
- **Filtro global do EF Core** por `comerciante_id` em ambos os serviços: o isolamento é o comportamento padrão, não algo que o desenvolvedor precisa lembrar.
- Policy `RequerComerciante` em todos os endpoints.
- No Keycloak, a claim é emitida por um *protocol mapper* a partir de um atributo do usuário (ou da conta de serviço, no fluxo Client Credentials).
- Modelo de dados: **tabelas compartilhadas com coluna discriminadora** (*shared schema*).

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Sistema de um comerciante só | Mais simples hoje, mas uma migração cara amanhã e irreal para produção. |
| `comerciante_id` na rota (`/comerciantes/{id}/lancamentos`) | Abre espaço para BOLA: exigiria conferir o id contra o token em todo endpoint. |
| Schema ou banco por comerciante | Isolamento mais forte, mas o custo operacional cresce com o número de clientes; justificável só com requisito regulatório. |

## Consequências

**Positivas**
- Pronto para SaaS sem retrabalho.
- O isolamento é garantido em três camadas: token, filtro global e índices/PK com `comerciante_id` na frente.

**Negativas**
- Todo índice relevante começa por `comerciante_id`.
- Operações administrativas entre comerciantes precisam desligar o filtro explicitamente (`IgnoreQueryFilters`), o que é auditável por busca no código.
