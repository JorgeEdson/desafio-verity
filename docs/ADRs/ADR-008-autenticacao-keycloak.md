# ADR-008 — Keycloak como IdP OIDC, e não IdP embutido na API

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

As duas APIs precisam autenticar o comerciante e obter seu `comerciante_id`. A referência interna de autenticação é o projeto *offcard-api*, em que a **própria API é o provedor de identidade**:

- login, refresh token persistido, logout e recuperação de senha por e-mail;
- `PasswordHasher` próprio;
- JWT **HS256** com chave simétrica;
- token transportado em cookie `HttpOnly` (frontend web);
- roles numéricas via atributo `OffCardAuthorize`.

São cerca de 900 linhas de código de identidade. Faz sentido lá: uma API única, com usuários e frontend web. Aqui o cenário é outro: **duas APIs atrás de um gateway, sem frontend.**

## Decisão

Usar **Keycloak** como Identity Provider OpenID Connect:

- realm `verity`, versionado em `infra/keycloak/realm-verity.json` e importado automaticamente no `docker compose up`;
- tokens **RS256**; as APIs obtêm a chave pública via JWKS (`options.Authority` apontando para o realm);
- audience `verity-api`; claim `comerciante_id` via *protocol mapper* de atributo de usuário;
- access token de 5 minutos;
- validação em **cada** API (`AddJwtBearer`): assinatura, issuer, audience, tempo de vida e `ClockSkew = 0`;
- em dev, client `verity-dev` com password grant **apenas** para testes e k6. Em produção, Authorization Code + PKCE para apps e Client Credentials para integrações ([Segurança](../04-seguranca.md)).

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **IdP embutido, no padrão offcard**, dentro de Lançamentos | O Consolidado passaria a depender de Lançamentos para autenticar, o que fere o espírito do RNF01. |
| **IdP embutido em um 4º serviço** (Identity) | Cerca de 900 linhas de código fora do domínio, mais um serviço e um banco de usuários; HS256 compartilhado entre três serviços (qualquer um poderia forjar tokens). Seria **mais** overengineering, não menos. |
| **Emissor só para desenvolvimento** (chave fixa) | Não representa como o sistema rodaria em produção. |
| **IdP gerenciado** (Entra ID, Cognito, Auth0) | É a melhor opção de produção quando não se quer operar identidade, mas não roda localmente para o avaliador. Fica como alternativa de produção: a troca é só configuração. |

## Consequências

**Positivas**
- **Zero código de identidade** nas APIs: só a configuração do `JwtBearer`.
- Chaves assimétricas: os serviços validam, mas não conseguem emitir tokens.
- Padrão aberto (OIDC): trocar o Keycloak por Entra ID ou Cognito muda `Authority`, `Audience` e o mapeamento da claim, sem mudar código.
- Refresh, MFA, força bruta e federação (Google, AD) ficam disponíveis sem desenvolvimento.

**Negativas**
- Mais um container no ambiente local (algumas centenas de MB de RAM).
- **Em produção, operar um IdP é responsabilidade séria:** cluster com 2+ instâncias, banco próprio, atualizações de segurança frequentes, backup e console administrativo protegido. Se a organização não quiser esse custo, use um IdP gerenciado.
- Se o IdP ficar fora, novos logins falham; tokens já emitidos seguem válidos até expirar (as chaves ficam em cache nas APIs).
- A equipe precisa conhecer os conceitos de OIDC/OAuth: realm, client, grant types e protocol mappers.
