# Keycloak — realm `verity`

Configuração do provedor de identidade (decisão em [ADR-008](../../docs/adr/ADR-008-autenticacao-keycloak.md)).
O realm é **versionado como código** em [`realm-verity.json`](realm-verity.json) e importado automaticamente quando o container sobe pela primeira vez.

## O que o realm contém

| Item | Configuração | Por quê |
|---|---|---|
| Assinatura | RS256 | As APIs só conhecem a chave pública (JWKS): validam, mas não conseguem emitir tokens |
| Access token | 5 minutos (padrão do realm) | Janela curta em caso de vazamento |
| Access token do client `verity-dev` | 30 minutos | Exceção só de desenvolvimento: permite executar a collection do Insomnia pasta a pasta sem renovar o token. Não se aplica a nenhum client de produção. |
| Refresh token | Rotação obrigatória (`revokeRefreshToken`, reuso 0) | Um refresh token roubado e reutilizado é detectado |
| Força bruta | Bloqueio temporário após 5 falhas | Proteção do login |
| Política de senha | Mínimo de 12 caracteres, diferente do usuário | |
| Perfil de usuário | Atributo `comerciante_id` declarado, **visível e editável só por administradores** | O comerciante não pode trocar o próprio `comerciante_id` e passar a ver dados de outro |
| Client `verity-dev` | Público, somente *password grant* | **Apenas desenvolvimento e testes** (ver abaixo) |
| Mapper `audience-verity-api` | Adiciona `verity-api` à claim `aud` | As APIs validam a audiência |
| Mapper `comerciante_id` | Copia o atributo do usuário para a claim `comerciante_id` do access token | Base do isolamento entre comerciantes ([ADR-007](../../docs/adr/ADR-007-multi-comerciante.md)) |

### Usuários de teste

| Usuário | Senha | `comerciante_id` |
|---|---|---|
| `loja.centro` | `Verity@2026!` | `c-001` |
| `loja.praia` | `Verity@2026!` | `c-002` |

Dois comerciantes existem para que os testes possam provar que um não enxerga os dados do outro.

## Obtendo um token

Com o compose de pé (`docker compose up -d`):

```bash
curl -s -X POST http://localhost:8081/realms/verity/protocol/openid-connect/token \
  -d grant_type=password \
  -d client_id=verity-dev \
  -d username=loja.centro \
  -d 'password=Verity@2026!'
```

PowerShell:

```powershell
$resposta = Invoke-RestMethod -Method Post `
  -Uri http://localhost:8081/realms/verity/protocol/openid-connect/token `
  -Body @{ grant_type='password'; client_id='verity-dev'; username='loja.centro'; password='Verity@2026!' }
$token = $resposta.access_token
```

Ao decodificar o `access_token` (em [jwt.io](https://jwt.io), por exemplo), espera-se:

```json
{
  "iss": "http://localhost:8081/realms/verity",
  "aud": "verity-api",
  "azp": "verity-dev",
  "comerciante_id": "c-001",
  "exp": "...",
  "sub": "..."
}
```

## Emissor (`iss`) e rede do compose

Um problema clássico com Keycloak em containers: o cliente pede o token por `localhost:8081`, mas as APIs falam com o Keycloak por `keycloak:8080`. Se o `iss` variar com o host, a validação falha.

A solução adotada reproduz o que se faz em produção:

- `KC_HOSTNAME=http://localhost:8081` fixa o emissor público. **Todo** token sai com `iss = http://localhost:8081/realms/verity`.
- `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true` faz os endpoints de *backchannel* (JWKS, token) seguirem o host da requisição. Quando a API busca o documento de descoberta por `http://keycloak:8080/realms/verity/.well-known/openid-configuration`, ela recebe o `jwks_uri` interno, acessível de dentro da rede.

Configuração que as APIs usarão (próximas etapas):

| Chave | Valor no compose |
|---|---|
| `Autenticacao:Emissor` | `http://localhost:8081/realms/verity` |
| `Autenticacao:EnderecoDeMetadados` | `http://keycloak:8080/realms/verity/.well-known/openid-configuration` |
| `Autenticacao:Audiencia` | `verity-api` |
| `Autenticacao:ExigirHttpsNosMetadados` | `false` (somente desenvolvimento) |

Em produção existe um único hostname público com TLS (ex.: `https://auth.verity.com.br`), e os quatro valores convergem para ele.

## Console administrativo

`http://localhost:8081/admin`, com o usuário e a senha definidos em `.env` (padrão: `admin`/`admin`).

## Alterando o realm

O import só acontece se o realm **ainda não existir**. Depois de editar o JSON:

```bash
docker compose rm -sf keycloak && docker compose up -d keycloak
```

(Em `start-dev` o banco é interno ao container: recriar o container reimporta o realm.)

## Diferenças para produção

| Desenvolvimento (este arquivo) | Produção |
|---|---|
| `start-dev`, banco embutido | `start`, PostgreSQL próprio, 2+ instâncias em cluster |
| HTTP | TLS obrigatório |
| `verity-dev` com password grant | `verity-app` com Authorization Code + PKCE; clients confidenciais com Client Credentials para integrações |
| Usuários e senhas fixos no JSON | Usuários criados por processo de onboarding; nenhum segredo versionado |
| Console administrativo exposto | Console acessível apenas por rede administrativa, com MFA |

Alternativa de produção: substituir o Keycloak por um IdP gerenciado (Entra ID, Cognito). As APIs não mudam, só a configuração da tabela acima e o mapeamento da claim `comerciante_id`.
