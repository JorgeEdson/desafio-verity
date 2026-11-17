# ADR-011 — Estrutura Api / Core / Infraestrutura, sem MediatR

- **Status:** Aceito; a duplicação aceita entre os serviços foi revista no [ADR-015](ADR-015-pacote-compartilhado-de-seguranca.md) (segurança) e no [ADR-016](ADR-016-blocos-de-construcao-compartilhados.md) (hospedagem, infraestrutura, testes e imagem)
- **Data:** 2026-11-16

## Contexto

O domínio de cada serviço é pequeno. Estruturas como a Clean Architecture "de template", com 4 a 6 projetos por serviço, CQRS com MediatR, AutoMapper e pipelines de behaviors, adicionariam cerimônia sem benefício proporcional. Ao mesmo tempo, queremos o domínio isolado de frameworks e testável.

O projeto *simple-erp* já tem uma organização validada que atende a isso.

## Decisão

Reaproveitar a estrutura do *simple-erp* em cada serviço:

| Projeto | Conteúdo | Depende de |
|---|---|---|
| `*.Core` | Entidades, objetos de valor (`Dinheiro`, `DataCompetencia`…), eventos de domínio, use cases, contratos (repositórios, `IUnitOfWork`, `IComercianteAtual`) | Somente `Verity.Compartilhado` |
| `*.Infraestrutura` | EF Core/Npgsql, repositórios, migrations, outbox, RabbitMQ | `Core` |
| `*.Api` / `*.Worker` | Host: endpoints, autenticação, DI, health checks, OpenTelemetry | `Core`, `Infraestrutura` |

Padrões reaproveitados: **`Resultado`** para falhas de negócio (sem exceções para fluxo), **dispatcher de use cases próprio**, **objetos de valor** com validação na construção e a base de **eventos de domínio**.

`Verity.Compartilhado` contém **apenas** primitivas técnicas e os contratos dos eventos de integração. Nenhuma regra de negócio.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **MediatR** | O dispatcher próprio do simple-erp cobre o necessário em poucas linhas, e o MediatR passou a ter licenciamento comercial em 2025. |
| **Clean Architecture com Domain e Application separados** | Separação útil em domínios grandes; aqui geraria projetos com 3 ou 4 classes cada. |
| **Vertical slices em um único projeto** | Boa opção para CRUDs. Perderíamos o isolamento do domínio em relação ao EF, que queremos testar sem banco. |
| **Projeto compartilhado de domínio entre os serviços** | Criaria um monolito distribuído: mudar o domínio de um contexto obrigaria a redeployar o outro. |

## Consequências

**Positivas**
- Estrutura conhecida, já testada no simple-erp, e coerente entre os dois serviços.
- O domínio é testável sem infraestrutura.

**Negativas**
- O `Verity.Compartilhado` é um ponto de acoplamento entre os serviços e precisa de disciplina: só primitivas e contratos, versionados.
- Um pouco de duplicação aceita entre os contextos (ex.: o enum `TipoLancamento` existe nos dois), em troca de independência.
