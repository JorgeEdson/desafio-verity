# ADR-015 — Pacote compartilhado de segurança das APIs

- **Status:** Aceito (revê a consequência "duplicação aceita" do [ADR-011](ADR-011-estrutura-interna-dos-servicos.md) para o código de segurança)
- **Data:** 2026-11-16

## Contexto

Com o Consolidado implementado, as duas APIs passaram a ter cópias idênticas de:

- configuração do JWT Bearer (emissor, audiência, metadados, `ClockSkew`);
- policy `RequerComerciante` e o nome da claim `comerciante_id`;
- leitura do comerciante a partir do token (`ComercianteAtualDaRequisicao`);
- o contrato `IComercianteAtual`, declarado separadamente em cada `Core`.

O ADR-011 aceitava duplicação entre contextos em troca de independência. Para **código de segurança**, porém, a duplicação é um risco específico: uma correção (por exemplo, reforçar a validação do token) aplicada a uma API e esquecida na outra abre uma brecha. Além disso, o código é puramente técnico e idêntico nos dois serviços, sem nenhuma regra de negócio que possa divergir.

## Decisão

1. Criar o projeto **`Verity.Compartilhado.Seguranca`** com:
   - `AdicionarAutenticacaoPorJwt` (configuração e validação do token) e `AutenticacaoOptions`;
   - `AdicionarAutorizacaoPorComerciante` (policy `RequerComerciante`, registro de `IComercianteAtual`);
   - `ComercianteAtualDaRequisicao`, `ClaimsDoToken` e `PoliticasDeAutorizacao`.
2. Mover o contrato **`IComercianteAtual`** para `Verity.Compartilhado` (namespace `Aplicacao`), para que os dois `Core` e a implementação compartilhada falem do mesmo tipo. O `Core` continua sem dependência de ASP.NET: só a implementação, que lê o `HttpContext`, fica no pacote de segurança.
3. Cada API apenas **chama** as extensões. O que é específico de cada serviço (mapeamento de códigos de erro para status HTTP, política de cache do Consolidado) permanece no serviço.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Manter a duplicação até surgir um terceiro serviço (ADR-011) | Para código de segurança, o risco de divergência pesa mais do que o acoplamento de um pacote técnico. |
| Colocar a segurança dentro de `Verity.Compartilhado` | Obrigaria o núcleo compartilhado, usado também pelos `Core`, a depender do ASP.NET. Projetos separados mantêm o `Core` livre de framework web. |
| Validar o token apenas no gateway | Contraria a defesa em profundidade do [ADR-009](ADR-009-gateway-yarp.md): o gateway não é fronteira de confiança. |
| Pacote NuGet publicado à parte | Faria sentido com repositórios separados por serviço; num monorepo, a referência de projeto dá o mesmo resultado sem pipeline de publicação. |

## Consequências

**Positivas**
- Uma única implementação da validação de token e do isolamento por comerciante.
- O Program.cs de cada API continua chamando os mesmos métodos (`AdicionarAutenticacaoPorJwt`, `AdicionarAutorizacaoPorComerciante`).

**Negativas**
- Uma mudança no pacote exige recompilar e reimplantar as duas APIs. É o comportamento desejado para segurança, mas é acoplamento de deploy.
- O pacote precisa de disciplina: só entra o que é técnico e idêntico entre as APIs. Regras de negócio, mapeamentos de erro e políticas de cache específicas ficam nos serviços.
