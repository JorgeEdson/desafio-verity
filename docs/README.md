# Documentação do projeto — Desafio Verity (Fluxo de Caixa)

Esta pasta concentra toda a documentação de arquitetura do projeto. O código vive em `src/`, mas as **decisões** vivem aqui: o desafio avalia principalmente a capacidade de decidir, justificar e registrar trade-offs.

## Como ler

| Ordem | Documento | Para quem / para quê |
|---|---|---|
| 1 | [Visão geral e domínio](01-visao-geral.md) | Problema, requisitos, premissas, regras de negócio e linguagem ubíqua |
| 2 | [Arquitetura da solução](02-arquitetura.md) | Diagramas C4 (contexto, containers, componentes), fluxos e modelo de dados |
| 3 | [Requisitos não funcionais](03-requisitos-nao-funcionais.md) | Metas mensuráveis e como cada uma é atendida e verificada |
| 4 | [Segurança](04-seguranca.md) | Autenticação, autorização, proteção de dados e superfície de ataque |
| 5 | [Implantação: local e produção](05-implantacao.md) | O que roda no `docker compose` e o que muda em produção |
| 6 | [Estratégia de testes](06-estrategia-de-testes.md) | Pirâmide de testes, testes de resiliência e de carga |
| 7 | [Evoluções futuras](07-evolucoes-futuras.md) | O que foi deliberadamente deixado de fora e quando passaria a valer a pena |
| 8 | [Convenções de código](08-convencoes-de-codigo.md) | Nomenclatura em português, sufixos de pattern/tecnologia, código sem comentários |
| — | [Registros de decisão (ADRs)](adr/README.md) | Uma decisão por arquivo, com contexto, alternativas e consequências |
| — | [Diagramas](diagramas/) | Fontes PlantUML dos diagramas C4: contexto, containers e componentes |

## Princípio que guia todas as decisões

> **Cada componente precisa ser justificado por um requisito.** Nada entra "porque é boa prática" ou "porque é moderno".

Os dois requisitos que realmente moldam a arquitetura são:

1. **O serviço de Lançamentos não pode ficar indisponível se o Consolidado cair.**
2. **O Consolidado deve suportar 50 req/s em pico com no máximo 5% de perda.**

Todo o resto (gateway, mensageria, outbox, cache, IdP) está rastreado até um desses requisitos ou até um requisito explícito do enunciado (segurança, escalabilidade, documentação). O que não passou nesse filtro está em [Evoluções futuras](07-evolucoes-futuras.md), com o gatilho que justificaria sua adoção.

## Enunciado original

O PDF do desafio está em [`desafio-arquiteto-software-jun25.pdf`](desafio-arquiteto-software-jun25.pdf).
