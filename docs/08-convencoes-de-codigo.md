# 8. Convenções de código

## 8.1 Princípio

**O código se explica sozinho.** Não há comentários no código-fonte nem nos arquivos de configuração: nada de `//`, `/* */`, XML docs (`///`), `#` em YAML/Dockerfile ou `<!-- -->` em `.csproj`.

A intenção que um comentário carregaria é expressa por:

| Em vez de... | Use... |
|---|---|
| Comentário explicando um bloco | Um método com nome que descreva o que o bloco faz (ex.: `builder.WebHost.ConfigurarServidorKestrel()`) |
| Comentário explicando um número ou texto fixo | Uma constante com nome descritivo (ex.: `TamanhoMaximoPadraoDoCorpoDaRequisicaoEmBytes`) |
| Comentário explicando uma condição | Uma variável ou um método booleano com nome descritivo |
| Comentário explicando *por que* uma decisão foi tomada | Um ADR em [`docs/adr`](adr/README.md) |
| Comentário explicando como configurar ou operar | O README do componente (ex.: [`infra/keycloak/README.md`](../infra/keycloak/README.md)) |

Comentários envelhecem e mentem; nomes são verificados pelo compilador a cada uso.

## 8.2 Nomenclatura

- **Português, verboso e sem abreviações**: `RegistrarLancamentoUseCase`, `CaminhosRecebidos`, `enderecoSemServidorEscutando`, `QuantidadeDeRequisicoesParaVerificarDistribuicao`.
- **Sufixos em inglês apenas para pattern ou tecnologia**, que são vocabulário técnico universal:

| Categoria | Sufixos | Exemplos |
|---|---|---|
| Pattern | `UseCase`, `Repository`, `Specification`, `Builder`, `Factory`, `Interceptor`, `Fake`, `Fixture` | `CadastrarClienteUseCase`, `ILancamentoRepository`, `ApiDeDestinoFake` |
| Tecnologia ou framework | `Extensions`, `Controller`, `WebApplicationFactory`, `DbContext`, `Async` | `HealthChecksExtensions`, `GatewayWebApplicationFactory`, `IniciarAsync` |
| Testes | `Tests` | `RoteamentoDoGatewayTests` |

- Os nomes de API do .NET e de bibliotecas permanecem como são (`MapReverseProxy`, `AddHealthChecks`). Os métodos de extensão da solução são em português (`AdicionarProxyReverso`, `MapearHealthChecksDoGateway`).
- As chaves de configuração da solução são em português (`Gateway:TamanhoMaximoDoCorpoEmBytes`). As de bibliotecas mantêm o formato exigido por elas (`ReverseProxy:Clusters:...`).

## 8.3 Estrutura de arquivos C#

- **Namespaces em bloco** (`namespace X { ... }`), um tipo por arquivo, nome do arquivo igual ao do tipo.
- `Program.cs` só **compõe** a aplicação, chamando métodos de extensão da pasta `Configuracao/`; a lógica de configuração fica nesses métodos.
- Classes `sealed` por padrão; `internal` para tipos que não precisam ser expostos.

## 8.4 Testes

- **Nome do método:** `Acao_DeveResultadoEsperado[_QuandoCondicao]`. Exemplos: `RequisicaoGet_DeveSerEncaminhadaAoServicoDeLancamentos_PreservandoCaminhoEQuery` e `RequisicaoAoServicoDeLancamentos_DeveRetornarBadGateway_QuandoDestinoEstiverForaDoAr`.
- Um comportamento por teste. As fases preparar/agir/verificar são separadas por linha em branco, não por comentário.
- Asserções com o próprio xUnit (`Assert`). O FluentAssertions foi evitado porque, a partir da versão 8, exige licença comercial.
