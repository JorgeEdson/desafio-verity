# ADR-016 — Blocos de construção técnicos compartilhados e Dockerfile único

- **Status:** Aceito (estende o [ADR-015](ADR-015-pacote-compartilhado-de-seguranca.md) e revê a consequência "duplicação aceita" do [ADR-011](ADR-011-estrutura-interna-dos-servicos.md))
- **Data:** 2026-11-16

## Contexto

Depois do ADR-015, ainda havia código técnico repetido entre os serviços:

| Código | Onde se repetia |
|---|---|
| `DispatcherUseCase` e `ControllerBaseDaApi` | Idênticos nas duas APIs |
| Mapeamento de códigos de erro para status HTTP, Problem Details | Mesma estrutura nas duas APIs, só os códigos mudavam |
| Health checks `/health/live` e `/health/ready` | Duas APIs e o gateway |
| Aplicação de migrations na inicialização | Duas APIs |
| `AddDbContext` com Npgsql, snake case e connection string lida de forma tardia | Duas infraestruturas e duas fixtures de teste |
| Design-time factory do EF | Duas infraestruturas |
| Opções e fábrica de conexão do RabbitMQ | Publicador (Lançamentos) e consumidor (Consolidado) |
| Emissor de tokens, factory de testes autenticada, imagens de containers | Dois projetos de teste |
| Dockerfile | Quatro arquivos quase iguais |

Toda essa duplicação é técnica: nenhuma linha contém regra de negócio. Mantê-la significa corrigir a mesma coisa em vários lugares (versão de imagem, comportamento do health check, formato de erro) e aceitar que divirjam sem intenção.

## Decisão

1. **`Verity.Compartilhado`** passa a conter o `DispatcherUseCase` (junto do contrato `IDispatcherUseCase`) e as etiquetas de verificação de saúde.
2. Novo **`Verity.Compartilhado.Hospedagem`** (depende de ASP.NET Core):
   - `ControllerBaseDaApi` e `ProblemaDeNegocioResult`, que produzem Problem Details com a extensão `erros`;
   - `StatusHttpDosCodigosDeErro`, alimentado por um dicionário que **cada serviço declara** (`StatusHttpDosErrosDeLancamentos`, `StatusHttpDosErrosDoConsolidado`);
   - `AdicionarDispatcherDeUseCases`, `AdicionarRespostasDeErroPadronizadas` e `MapearHealthChecksDeVivacidadeEProntidao`, usados também pelo gateway.
3. Novo **`Verity.Compartilhado.Infraestrutura`** (EF Core + Npgsql + RabbitMQ.Client):
   - `AdicionarBancoPostgreSql<TDbContext>`, `AdicionarVerificacaoDeSaudeDoBanco<TDbContext>`, `AplicarMigracoesAsync<TDbContext>` e `OpcoesDoPostgreSql`;
   - `PostgreSqlDesignTimeDbContextFactory<TDbContext>`, base das factories de design-time;
   - `ConexaoRabbitMqOptions` com `CriarFabricaDeConexoes()` e `AdicionarConexaoRabbitMq`. As configurações exclusivas do consumidor foram para `ConsumoDeLancamentosRegistradosOptions` (seção `ConsumoDeLancamentosRegistrados`).
4. Novo **`tests/Verity.Testes.Compartilhado`**: `EmissorDeTokensDeTeste`, `ComercianteAtualFake`, `ApiAutenticadaWebApplicationFactory<TProgram>` e `ConteineresDeTeste`. As factories de cada serviço declaram só a própria configuração.
5. **Um `Dockerfile` na raiz**, parametrizado por `PROJETO` e `ASSEMBLY`, usado pelos quatro serviços no `docker-compose.yml`.

### O que continua em cada serviço

Domínio, use cases, repositórios, SQL de consolidação, outbox, consumo de mensagens, política de cache, rotas, tabela de status HTTP dos próprios erros, migrations e appsettings. A regra para entrar num projeto compartilhado é: **técnico, sem regra de negócio, e igual em pelo menos dois serviços**.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Manter a duplicação (ADR-011) | O custo de corrigir em vários lugares já apareceu em código idêntico; não havia ganho de independência real, porque os serviços estão no mesmo repositório e são implantados pelo mesmo pipeline. |
| Um único projeto compartilhado com tudo | Obrigaria o `Core` e o gateway a carregar EF Core e RabbitMQ. A divisão em Hospedagem (web) e Infraestrutura (dados e mensageria) mantém cada consumidor com só o que usa: o gateway referencia apenas a Hospedagem. |
| Unificar também o dicionário de status HTTP | Os códigos de erro são do domínio de cada serviço; o mapeamento fica com o serviço, só o mecanismo é compartilhado. |
| Pacotes NuGet publicados | Mesmo argumento do ADR-015: no monorepo, a referência de projeto resolve sem pipeline de publicação. |
| Dockerfile por serviço | Quatro cópias do mesmo processo de build; qualquer ajuste (imagem base, usuário, cache) teria que ser repetido. |

## Consequências

**Positivas**
- O `Program.cs` de cada API fica declarativo: só chama extensões.
- Uma correção em formato de erro, health check, conexão com banco ou RabbitMQ vale para todos os serviços de uma vez.
- Novos testes de integração herdam autenticação e containers prontos.

**Negativas**
- Mudanças nos projetos compartilhados exigem reconstruir todos os serviços que os referenciam. Num monorepo com pipeline único, é o comportamento esperado.
- Os projetos compartilhados podem virar depósito de qualquer coisa. A regra acima (técnico, sem negócio, usado por dois ou mais) é o filtro, verificado em revisão de código.
- O `Dockerfile` único copia todo o `src/` para restaurar; o cache de camadas é menos granular do que copiar só os `.csproj`. O cache de pacotes do BuildKit compensa a maior parte do custo.
