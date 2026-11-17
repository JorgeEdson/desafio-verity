# Collection do Insomnia — teste de ponta a ponta

`verity.insomnia.json` exercita o cenário completo pelo **gateway** (`http://localhost:8080`), autenticando no **Keycloak** (`http://localhost:8081`). Cada requisição tem testes no script *after-response*, e o Collection Runner mostra o resultado de todos.

Requer **Insomnia 9.3 ou superior** (scripts pré-requisição e pós-resposta).

## Como usar

1. Suba o ambiente na raiz do repositório:
   ```powershell
   docker compose up -d --build
   docker compose ps
   ```
   Espere todos os serviços ficarem `healthy` ou `running`. O Keycloak leva uns 30 segundos para importar o realm.
2. No Insomnia: **Import** → selecione `tests/insomnia/verity.insomnia.json`.
3. Abra o **Collection Runner**, selecione as pastas **01 a 09**, defina um atraso de **500 ms** entre requisições e execute.
4. A pasta **10** é manual: siga a descrição de cada requisição, que indica quando parar e iniciar containers.

A collection pode ser executada várias vezes seguidas. Cada execução gera um identificador próprio para as chaves de idempotência e confere os saldos **em relação à linha de base** lida no início, então lançamentos de execuções anteriores não atrapalham.

## O que cada pasta comprova

| Pasta | Comprova |
|---|---|
| 01 · Preparação e saúde | Gateway com `/health/live` e `/health/ready`; emissor do Keycloak igual ao configurado nas APIs. Gera as datas no fuso `America/Sao_Paulo`. |
| 02 · Autenticação | Token RS256 com audiência `verity-api` e a claim `comerciante_id` correta para cada loja; senha errada recusada. |
| 03 · Segurança nas APIs | Requisição sem token ou com assinatura adulterada recebe 401. |
| 04 · Linha de base | Lê os saldos antes dos lançamentos desta execução. |
| 05 · Lançamentos | Crédito e débito do dia, crédito retroativo (D-3), consulta por id com `Location`, extrato paginado. |
| 06 · Idempotência e estorno | Repetição com a mesma chave devolve 200 e o mesmo id; chave reaproveitada com outro conteúdo dá 422; falta de chave dá 400; estorno, repetição do estorno, estorno duplicado (409) e estorno de estorno (409). |
| 07 · Validações | Data futura, valor zero, três casas decimais, tipo inválido, período acima de 93 dias. |
| 08 · Isolamento | A loja.praia recebe 404 ao ler ou estornar lançamento da loja.centro, e o extrato dela não traz esses lançamentos. |
| 09 · Consolidado | Consistência eventual: saldo de hoje = base + 1.300,00 (incluindo o retroativo propagado), dia retroativo = base + 300,00, período com os 7 dias preenchidos e encadeados. |
| 10 · Resiliência (manual) | Uma réplica do consolidado fora: o gateway continua respondendo. Consolidado e worker fora: lançamentos continuam com 201. Na volta, o worker consolida o que ficou na fila. |

## Observações

- As consultas ao consolidado enviam o parâmetro `leitura` com um valor novo a cada vez. A política de cache varia por parâmetro de consulta, então isso evita reaproveitar a resposta em cache (5 s) e permite conferir o saldo logo depois do processamento.
- A primeira consulta da pasta 09 aguarda 4 segundos antes de enviar, por causa da consistência eventual (outbox → RabbitMQ → worker). Se o ambiente estiver lento e algum teste dessa pasta falhar, reenvie a requisição.
- O token emitido para o client `verity-dev` vale 30 minutos (exceção de desenvolvimento; o padrão do realm é 5). Se passar disso, execute a pasta 02 de novo.
- O client `verity-dev` usa *password grant* **somente** para desenvolvimento e testes (ver [ADR-008](../../docs/adr/ADR-008-autenticacao-keycloak.md)).
