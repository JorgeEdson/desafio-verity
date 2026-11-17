# 1. Visão geral e domínio

## 1.1 Problema

Um comerciante precisa controlar seu fluxo de caixa diário registrando **lançamentos** (débitos e créditos) e precisa de um relatório com o **saldo diário consolidado**.

## 1.2 Requisitos do enunciado

| Tipo | Requisito |
|---|---|
| Negócio | Serviço de controle de lançamentos |
| Negócio | Serviço do consolidado diário |
| Técnico | Desenho da solução, C#, testes, boas práticas, README, repositório público, documentação no repositório |
| Não funcional | O serviço de Lançamentos **não deve ficar indisponível** se o Consolidado cair |
| Não funcional | Em pico, o Consolidado recebe **50 req/s**, com **no máximo 5% de perda** |

## 1.3 Linguagem ubíqua

| Termo | Significado |
|---|---|
| **Comerciante** | Dono da conta de fluxo de caixa. Todo dado pertence a exatamente um comerciante, identificado por `ComercianteId`. |
| **Lançamento** | Registro de um movimento financeiro: um **crédito** (entrada) ou um **débito** (saída), com valor positivo e data de competência. |
| **Data de competência** | Dia ao qual o lançamento pertence para fins de saldo. Pode ser anterior à data de registro (lançamento retroativo). |
| **Estorno** | Lançamento de natureza inversa que anula um lançamento anterior. É a única forma de "corrigir" um lançamento. |
| **Saldo de abertura** | Primeiro lançamento do comerciante, que representa quanto havia na conta antes de começar a usar o sistema. |
| **Movimento do dia** | Soma dos créditos menos a soma dos débitos com competência naquele dia. |
| **Saldo diário consolidado** | **Saldo de fechamento do dia**: saldo do dia anterior + créditos do dia − débitos do dia. É a "fotografia" de quanto havia disponível ao final daquele dia. |

### Exemplo

| Movimentação | Valor |
|---|---|
| Saldo inicial do dia (fechamento do dia anterior) | R$ 5.000,00 |
| Recebimento de salário (crédito) | + R$ 8.000,00 |
| Pagamento de conta (débito) | − R$ 1.200,00 |
| Transferência recebida (crédito) | + R$ 500,00 |
| Pagamento de cartão (débito) | − R$ 2.000,00 |
| **Saldo diário consolidado** | **R$ 10.300,00** |

## 1.4 Regras de negócio

| # | Regra |
|---|---|
| RN01 | Um lançamento tem **tipo** (`Credito` ou `Debito`), **valor > 0** com duas casas decimais, **data de competência** e **descrição** obrigatória. |
| RN02 | Lançamentos são **imutáveis**: não há edição nem exclusão. Correções são feitas por **estorno**. |
| RN03 | O estorno cria um novo lançamento de tipo inverso, mesmo valor e **mesma data de competência** do original, com referência a ele. |
| RN04 | Um lançamento pode ser estornado **uma única vez**, e um estorno não pode ser estornado. |
| RN05 | É permitido registrar lançamentos **retroativos** (competência anterior a hoje). Nesse caso, o saldo de fechamento do dia de competência **e de todos os dias posteriores** é recalculado. |
| RN06 | A data de competência **não pode estar no futuro**. "Hoje" é avaliado no fuso do comerciante (premissa P03). |
| RN07 | Um dia sem lançamentos tem saldo de fechamento igual ao do último dia com movimento anterior a ele, e movimento zero. |
| RN08 | O saldo **pode ficar negativo**: o sistema registra o fluxo de caixa, não autoriza pagamentos. |
| RN09 | Um comerciante só enxerga e altera os próprios dados. |

## 1.5 Premissas

Premissas são decisões tomadas onde o enunciado é silencioso. Cada uma pode ser revista sem mudar a arquitetura.

| # | Premissa |
|---|---|
| P01 | Cada comerciante tem **uma única conta** de fluxo de caixa. Múltiplas contas por comerciante são uma evolução (ver [Evoluções](07-evolucoes-futuras.md)). |
| P02 | Moeda única (BRL). |
| P03 | O fuso horário padrão do comerciante é `America/Sao_Paulo`. |
| P04 | O Consolidado é **eventualmente consistente** com os Lançamentos. O atraso alvo é de poucos segundos (ver [RNF](03-requisitos-nao-funcionais.md)), aceitável para um relatório de saldo. |
| P05 | Os consumidores da solução são clientes de API (um app do comerciante ou integrações). Não há frontend no escopo. |
| P06 | Os 50 req/s de pico do Consolidado são **leituras do relatório**. |

## 1.6 Fora de escopo

- Conciliação bancária, categorias de lançamento, anexos e comprovantes.
- Cadastro e autogestão de comerciantes. A identidade é responsabilidade do IdP (ver [ADR-008](adr/ADR-008-autenticacao-keycloak.md)).
- Frontend.
