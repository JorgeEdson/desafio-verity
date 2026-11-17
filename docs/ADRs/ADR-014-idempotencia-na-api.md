# ADR-014 — Idempotência obrigatória nos comandos de Lançamentos

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

Em um sistema financeiro, **um lançamento duplicado é um erro de saldo**. As causas mais comuns não vêm do servidor, e sim do caminho até ele:

- o cliente envia o `POST`, o lançamento é gravado, mas a resposta se perde (timeout, queda de rede) e o cliente reenvia;
- retry automático em bibliotecas HTTP, proxies ou no próprio app;
- duplo clique ou duplo envio na interface;
- *replay* de uma requisição capturada.

O [ADR-004](ADR-004-consumo-idempotente.md) protege o caminho **entre os serviços**. Este ADR protege a **entrada** do sistema. Sem os dois, a garantia de "nenhum lançamento duplicado" fica incompleta.

## Decisão

1. O header **`Idempotency-Key` é obrigatório** em `POST /lancamentos` e `POST /lancamentos/{id}/estornos`. A ausência retorna `400`. O cliente gera um valor único por **intenção** de operação (recomendado: UUID v4) e o reutiliza em todas as tentativas daquela mesma operação.
2. A chave é gravada **na própria linha do lançamento**, junto com `hash_da_requisicao` (SHA-256 do corpo normalizado), com **índice único `(comerciante_id, chave_de_idempotencia)`**. A chave tem escopo por comerciante: chaves iguais de comerciantes diferentes não colidem.
3. Comportamento:

| Situação | Resposta |
|---|---|
| Chave nova | Processa normalmente → `201` |
| Chave já usada, mesmo hash | Não processa; devolve o lançamento original → `200` |
| Chave já usada, hash diferente | `422` (uso indevido da chave) |
| Duas requisições simultâneas com a mesma chave | O índice único rejeita a segunda inserção; ela é tratada como "chave já usada" e devolve o lançamento da primeira |

4. Como a verificação e a gravação acontecem na **mesma transação** do lançamento e do outbox, um evento duplicado também nunca é gerado.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **Header opcional** | Deixa a proteção a critério do cliente; basta um integrador desatento para gerar duplicatas. |
| **Deduplicação por conteúdo** (mesmo valor, data e descrição em uma janela de tempo) | Gera falsos positivos: dois pagamentos legítimos e idênticos no mesmo dia são comuns em fluxo de caixa. |
| **Tabela separada de chaves**, com TTL e resposta armazenada | É o padrão genérico (estilo Stripe), útil quando muitos endpoints diferentes precisam de idempotência. Aqui são dois comandos que já criam um lançamento; guardar a chave na própria linha é mais simples e não expira. |
| **Cache distribuído** (Redis) para as chaves | Não é transacional com o banco: a janela entre "verifica no cache" e "grava no banco" permite duplicatas. |
| **Middleware genérico de idempotência** | Abstração maior que o problema; o tratamento no use case é explícito e testável. |

## Consequências

**Positivas**
- Retries são seguros por construção: o cliente pode repetir à vontade.
- A garantia final está no **banco** (índice único), não só no código: é correta mesmo sob concorrência e com várias réplicas da API.
- Junto com o ADR-004, cobre a duplicidade de ponta a ponta: entrada → outbox → broker → consolidado.

**Negativas**
- **Contrato mais exigente para o cliente**, que precisa gerar e guardar a chave durante as tentativas. Documentado no README e na especificação OpenAPI.
- Duas colunas a mais por lançamento.
- As chaves não expiram. Para este domínio isso é desejável: um retry tardio continua seguro.
