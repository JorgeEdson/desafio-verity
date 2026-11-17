# Teste de carga do Consolidado (k6)

`consolidado-50rps.js` comprova o requisito não funcional do enunciado: **o Consolidado recebe 50 requisições por segundo em pico, com no máximo 5% de perda** (RNF02–RNF04 em [`docs/03-requisitos-nao-funcionais.md`](../../docs/03-requisitos-nao-funcionais.md)).

## O que o script faz

1. **Preparação (`setup`)**: obtém um token da `loja.praia` no Keycloak e registra, pela API de Lançamentos, um lançamento por dia nos últimos **90 dias** (créditos e débitos), com chaves de idempotência próprias da execução.
2. **Carga**: executor `constant-arrival-rate`, **50 requisições por segundo durante 2 minutos**, pelo gateway, em `GET /consolidado/diario/{data}`. Cada requisição consulta um dia aleatório dos 90, para não medir só o cache.
3. **Critérios (*thresholds*)**, que fazem o k6 terminar com erro se não forem atingidos:

| Métrica | Critério | Origem |
|---|---|---|
| `http_req_failed` | `< 5%` (ou `< 1%` com `TAXA_DE_ERRO_MAXIMA=0.01`) | Enunciado / meta interna |
| `http_req_duration` p95 | `< 200 ms` | RNF04 |
| `http_req_duration` p99 | `< 500 ms` | RNF04 |
| `dropped_iterations` | `0` | Garante que os 50 req/s foram de fato gerados |

Os critérios valem só para o cenário de leitura; as requisições da preparação não entram na conta.

## Como executar

Com o ambiente no ar (`docker compose up -d`), o k6 roda em container na mesma rede, sem instalar nada:

```powershell
docker compose --profile carga run --rm k6 run consolidado-50rps.js
```

Meta interna (perda abaixo de 1%):

```powershell
docker compose --profile carga run --rm -e TAXA_DE_ERRO_MAXIMA=0.01 -e CENARIO=consolidado-50rps-meta-interna k6 run consolidado-50rps.js
```

### Variação com falha

Comprova que a queda de uma réplica não derruba o serviço:

1. Inicie a execução com outro nome de cenário:
   ```powershell
   docker compose --profile carga run --rm -e CENARIO=consolidado-50rps-com-falha k6 run consolidado-50rps.js
   ```
2. Quando a carga começar (a barra de progresso do cenário `leitura_do_consolidado` aparece depois da preparação), espere uns **60 segundos** e, em outro terminal:
   ```powershell
   docker compose stop consolidado-api-1
   ```
3. Ao final, religue a réplica: `docker compose start consolidado-api-1`.

O esperado é continuar dentro dos *thresholds*: o gateway tira a réplica parada da rotação pelo health check passivo e ativo, e as poucas requisições em voo no momento da queda ficam muito abaixo de 5%.

## Resultados

O resumo de cada execução é gravado em [`resultados/`](resultados/) (`.txt` legível e `.json` completo) e fica versionado como evidência. Durante a execução, o Aspire Dashboard (http://localhost:18888) mostra a latência por réplica e as métricas `aspnetcore.rate_limiting.*`.

## Parâmetros

| Variável | Padrão | Uso |
|---|---|---|
| `GATEWAY_URL` | `http://gateway:8080` | Endereço do gateway na rede do compose |
| `KEYCLOAK_URL` | `http://keycloak:8080` | Endereço do Keycloak na rede do compose |
| `USUARIO` / `SENHA` | `loja.praia` / `Verity@2026!` | Comerciante usado na carga |
| `TAXA_DE_ERRO_MAXIMA` | `0.05` | Critério de perda |
| `CENARIO` | `consolidado-50rps` | Prefixo dos arquivos de resultado |
