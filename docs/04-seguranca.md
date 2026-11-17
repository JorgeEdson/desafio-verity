# 4. Segurança

## 4.1 Identidade e autenticação

- A **emissão** de tokens é responsabilidade exclusiva de um IdP OpenID Connect: Keycloak neste projeto, substituível por Entra ID, Cognito ou Auth0 apenas mudando configuração ([ADR-008](adr/ADR-008-autenticacao-keycloak.md)).
- Os tokens são **JWT assinados com RS256**. As APIs só conhecem a **chave pública**, obtida automaticamente do endpoint JWKS do realm. Nenhum serviço consegue forjar tokens.
- Cada API valida o token **sozinha**: assinatura, `iss`, `aud` (`verity-api`), `exp`/`nbf` e `ClockSkew = 0`. O gateway **não** é uma fronteira de confiança (defesa em profundidade; ver [ADR-009](adr/ADR-009-gateway-yarp.md)).
- O access token é curto (5 min); a renovação é feita com refresh token, gerenciado pelo IdP. A única exceção é o client de desenvolvimento `verity-dev`, com 30 min, para facilitar testes manuais; ele não existe no realm de produção.

### Fluxos OAuth por ambiente

| Ambiente | Cliente | Fluxo |
|---|---|---|
| Desenvolvimento / testes / k6 | `verity-dev` | *Resource Owner Password Credentials*. **Somente no realm de desenvolvimento.** Removido no OAuth 2.1 e inaceitável em produção. |
| Produção — app do comerciante | `verity-app` (público) | Authorization Code + **PKCE** |
| Produção — integração entre sistemas | `verity-integracao-<parceiro>` (confidencial) | Client Credentials. A conta de serviço carrega o atributo `comerciante_id`. |

## 4.2 Autorização e isolamento entre comerciantes

A maior ameaça em uma API multi-cliente é o **BOLA** (*Broken Object Level Authorization*, primeiro item do OWASP API Security Top 10), ou seja, um comerciante acessar dados de outro trocando um id. Mitigação:

- O `comerciante_id` vem **exclusivamente** da claim do token, via `IComercianteAtual`. Nunca é aceito na rota, na query ou no corpo.
- Toda consulta e todo comando dos repositórios filtram por `comerciante_id`. Um **filtro global do EF Core** garante isso por padrão, de modo que esquecer o filtro não abre os dados de outro comerciante.
- Os endpoints exigem a policy `RequerComerciante`: o token precisa conter a claim `comerciante_id`.
- Acessar um recurso de outro comerciante retorna `404`, não `403`, para não revelar que o recurso existe.

## 4.3 Proteção de dados

| Camada | Desenvolvimento | Produção |
|---|---|---|
| Em trânsito, cliente → borda | HTTP | TLS 1.2+ terminado no load balancer; HSTS |
| Em trânsito, interno | HTTP na rede do compose | TLS entre serviços, ou rede privada + mTLS via plataforma |
| Em repouso | Volumes locais | Criptografia do disco gerenciado (Postgres e RabbitMQ) |
| Segredos | `.env` (fora do Git, com `.env.example` versionado) | Key Vault / Secrets Manager, injetados como variáveis de ambiente |

Os dados tratados (valores e descrições de lançamentos) não incluem dados pessoais sensíveis. O `comerciante_id` é um identificador opaco. Os logs **não** registram o corpo das requisições nem os tokens.

## 4.4 Princípio do menor privilégio

| Recurso | Lançamentos | Consolidado API | Consolidado Worker |
|---|---|---|---|
| lancamentos_db | leitura e escrita | — | — |
| consolidado_db | — | **somente leitura** | leitura e escrita |
| RabbitMQ | publicar na exchange `lancamentos` | — | consumir da fila `consolidado.lancamentos` |

Cada serviço tem usuário próprio no banco e no RabbitMQ. A migration de esquema roda com um usuário separado, que não é o da aplicação em produção.

## 4.5 Proteção contra abuso

- **Validação de entrada** em todo comando (tipos, faixas, tamanho da descrição) e rejeição de campos desconhecidos.
- **Limite de tamanho do payload** no gateway.
- **Rate limiting por comerciante**, aplicado **nas APIs** (depois da autenticação, porque precisa da claim), com *token bucket*: Consolidado com rajada de 200 e reposição de 100 req/s; Lançamentos com rajada de 100 e reposição de 50 req/s. O excesso recebe `429` com `Retry-After` e o código `LIMITE_DE_REQUISICOES_EXCEDIDO`. O limite fica muito acima da carga nominal, para não consumir o orçamento de 5% de perda do RNF03 ([ADR-017](adr/ADR-017-limite-de-requisicoes-por-comerciante.md)).
- **SQL injection:** acesso exclusivamente via EF Core com parâmetros; o SQL bruto da consolidação também é parametrizado.
- **`Idempotency-Key` obrigatório** nos comandos de Lançamentos, garantido por índice único no banco. Impede lançamentos duplicados por retry, por duplo envio ou por *replay* de uma requisição capturada ([ADR-014](adr/ADR-014-idempotencia-na-api.md)).
- No Keycloak: detecção de força bruta, política de senha, MFA para administradores e console administrativo **não exposto** à internet.

## 4.6 Auditoria

Lançamentos imutáveis ([ADR-006](adr/ADR-006-lancamentos-imutaveis.md)) já constituem a trilha de auditoria do domínio: nada é apagado ou sobrescrito, e cada correção é um estorno rastreável até o original. Os eventos de autenticação ficam registrados no Keycloak.
