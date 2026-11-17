# 7. Evoluções futuras

O que foi **deliberadamente** deixado de fora, com o **gatilho** que justificaria a adoção. Não fazer agora é uma decisão, não um esquecimento.

## 7.1 Evoluções funcionais

| Evolução | Descrição | Gatilho |
|---|---|---|
| Múltiplas contas por comerciante | `ContaId` nos lançamentos e no saldo; consolidado por conta e total | Pedido de negócio |
| Categorias e centro de custo | Classificar lançamentos e consolidar por categoria | Relatórios gerenciais |
| Lançamentos futuros (previstos) | Fluxo de caixa projetado, separado do realizado | Pedido de negócio |
| Fechamento de período | Bloquear retroativos antes de uma data de fechamento contábil | Exigência contábil ou fiscal |
| Importação de extrato (OFX/CNAB) | Ingestão em lote, com conciliação | Volume de lançamentos manuais |

## 7.2 Evoluções técnicas

| Evolução | Por que não agora | Gatilho para adotar |
|---|---|---|
| **Job de reconciliação** | Os testes R4–R7 já cobrem a exatidão | Primeiro ambiente com dados reais. É a **primeira evolução** que eu implementaria antes de ir para produção. |
| **Reconstrução do consolidado** | Exige um endpoint/ferramenta administrativa para republicar eventos a partir dos lançamentos | Mudança no modelo de leitura, ou perda do consolidado_db |
| **Cache distribuído (Redis)** | Com TTL de 5 s e 50 req/s, um cache em memória por réplica é suficiente; o Redis traria mais uma dependência e mais um ponto de falha | Muitas réplicas com baixa taxa de acerto, ou carga uma ordem de grandeza acima |
| **Réplica de leitura do Postgres** para a Consolidado API | A leitura é uma consulta pela PK; o servidor primário aguenta com folga | CPU do consolidado_db de forma sustentada > 60% |
| **Particionamento de `saldo_diario`** por comerciante | Volume pequeno (≈ 365 linhas por comerciante por ano) | Dezenas de milhões de linhas |
| **Kafka** no lugar do RabbitMQ | Não precisamos de log reprocessável nem de particionamento ordenado | Vários consumidores novos precisando reprocessar o histórico, ou vazão de milhares de eventos por segundo |
| **Event sourcing** em Lançamentos | Lançamentos imutáveis já dão trilha de auditoria; ES acrescentaria complexidade sem requisito | Necessidade de reconstruir estados arbitrários do passado |
| **Kubernetes / service mesh** | Container Apps ou ECS atendem com muito menos operação; mTLS pode vir da plataforma | Dezenas de serviços, ou exigência de políticas de rede finas |
| **Testes de arquitetura** (NetArchTest) | A regra de dependência é simples e está garantida pelas referências entre projetos | Crescimento do time |
| **Limite de requisições distribuído (Redis)** | O limite atual é por réplica ([ADR-017](adr/ADR-017-limite-de-requisicoes-por-comerciante.md)); com limites folgados e poucas réplicas, a imprecisão é aceitável | Necessidade de teto exato por comerciante (planos pagos, cotas contratuais) ou muitas réplicas |
| **Rate limiting no gateway** | Precisa da identidade do comerciante, e o gateway não valida token | Ataques volumétricos sem autenticação (tratar no WAF) |
| **Contract testing** (Pact) entre os contextos | Um único evento, num monorepo, com contrato no projeto compartilhado | Contextos em repositórios ou times diferentes |
| **Versionamento de API** (`/v1`) | Um único consumidor por enquanto | Primeiro consumidor externo de produção |
