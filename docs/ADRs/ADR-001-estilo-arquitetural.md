# ADR-001 — Estilo arquitetural: dois serviços independentes

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

O enunciado pede um serviço de lançamentos e um serviço de consolidado diário, e impõe: *"o serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário cair"*. O domínio é pequeno: um agregado (`Lancamento`) e um modelo de leitura (`SaldoDiario`).

Esse requisito é de **isolamento de falha em tempo de execução**. Isolamento de código (módulos) não basta: se os dois rodam no mesmo processo, um vazamento de memória, uma exceção não tratada ou um pico de leitura no consolidado derruba ou degrada também os lançamentos.

## Decisão

Dois **contextos delimitados**, Lançamentos e Consolidado, cada um implantável de forma independente, com banco próprio e **sem nenhuma chamada síncrona entre eles**. O Consolidado tem dois processos (API de leitura e Worker de consolidação) que compartilham o mesmo código de domínio.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **Monolito** (um processo, um banco) | Não atende o requisito de isolamento: uma falha no consolidado é uma falha no processo inteiro. |
| **Monolito modular** (um processo, módulos isolados) | Resolve o acoplamento de código, mas não o de execução. Seria minha escolha se não houvesse esse RNF. |
| **Microsserviços granulares** (ex.: serviço de categorias, de relatórios, de usuários…) | Não há requisito que justifique mais fronteiras; cada serviço novo adiciona deploy, banco, observabilidade e falhas parciais. |
| **Serverless** (Functions/Lambda) | Viável para o Worker, mas *cold start* pesa no p95 da leitura, e o desenvolvimento e os testes locais ficam mais complexos para o avaliador. |
| **Consolidado lendo o banco de Lançamentos** | Acopla os dois pelo esquema e põe a carga de leitura sobre o banco transacional. |

## Consequências

**Positivas**
- O RNF de disponibilidade é atendido por construção, não por configuração.
- Cada lado escala de forma independente: a leitura com 50 req/s não disputa recursos com a escrita.

**Negativas**
- **Consistência eventual** entre os contextos (ver [ADR-002](ADR-002-comunicacao-assincrona-rabbitmq.md)).
- Mais peças para operar do que num monolito: dois bancos, um broker, dois deploys.
- É preciso garantir a entrega dos eventos ([ADR-003](ADR-003-transactional-outbox.md), [ADR-004](ADR-004-consumo-idempotente.md)).
