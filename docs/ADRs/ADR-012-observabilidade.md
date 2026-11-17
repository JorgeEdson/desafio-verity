# ADR-012 — Observabilidade com OpenTelemetry

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

Num fluxo assíncrono, a pergunta operacional mais comum é: *"registrei o lançamento; por que o saldo ainda não mudou?"*. Responder exige seguir uma requisição através do gateway, da API, do outbox, do broker e do Worker. O enunciado pede ainda monitoramento proativo e métricas de confiabilidade, integridade e disponibilidade.

## Decisão

**OpenTelemetry** em todos os serviços (traces, métricas e logs), exportando via OTLP:

- **Traces distribuídos de ponta a ponta:** o `traceparent` é gravado na coluna `contexto_de_rastreamento` do outbox, propagado nos headers da mensagem e restaurado pelo Worker. Um único trace cobre do `POST /lancamentos` até a atualização do `saldo_diario`.
- **Métricas de negócio e de operação:** eventos pendentes no outbox, idade do evento pendente mais antigo, eventos processados pelo Worker por resultado, duração da consolidação e atraso entre o registro e a consolidação, mais as métricas HTTP, de runtime e do Npgsql. A profundidade da fila e da DLQ vem do próprio RabbitMQ (console de gerenciamento localmente; plugin Prometheus do broker em produção), sem duplicar essa medição na aplicação.
- **Logs estruturados** correlacionados por `TraceId`, sem corpo de requisição e sem tokens.
- **Health checks** `/health/live` (o processo está de pé) e `/health/ready` (dependências acessíveis), usados pelo gateway e pelo orquestrador.
- Local: **Aspire Dashboard** (um container). Produção: OTel Collector → backend escolhido (Azure Monitor, Grafana etc.), com alertas definidos em [RNF §3.4](../03-requisitos-nao-funcionais.md).

## Implementação

| Onde | O quê |
|---|---|
| `Verity.Compartilhado.Observabilidade` | `AdicionarObservabilidade(nomeDoServico)`, chamado no `Program.cs` dos quatro serviços. Registra traces (ASP.NET Core, HttpClient, Npgsql e as fontes `Verity.*`), métricas (ASP.NET Core, HttpClient, runtime, Npgsql e os medidores `Verity.*`) e logs pelo OpenTelemetry. As verificações de saúde ficam fora dos traces. O exportador OTLP só é ligado quando `OTEL_EXPORTER_OTLP_ENDPOINT` está definido, então os testes e a execução local sem o dashboard não tentam exportar. |
| `Verity.Compartilhado` → `RastreamentoDeMensageria` | Fonte de atividades `Verity.Mensageria`. O publicador abre um span *producer* filho do `traceparent` gravado no outbox e envia o próprio contexto no header da mensagem; o Worker abre um span *consumer* a partir desse header. As consultas do Npgsql feitas durante a consolidação ficam dentro desse span. |
| Lançamentos → `MetricasDoOutbox` | `verity.outbox.eventos_publicados`, `verity.outbox.eventos_pendentes` e `verity.outbox.idade_do_evento_pendente_mais_antigo`, medidos pelo relay a cada ciclo. A contagem usa o índice parcial de pendentes. |
| Worker → `MetricasDaConsolidacao` | `verity.consolidacao.eventos_processados` (etiqueta `resultado`: `consolidado`, `duplicado`, `rejeitado`), `verity.consolidacao.duracao` e `verity.consolidacao.atraso_desde_o_registro`. |
| `docker-compose.yml` | Container `aspire-dashboard` (interface em http://localhost:18888). Todos os serviços recebem `OTEL_EXPORTER_OTLP_ENDPOINT` e intervalos de exportação curtos para o ambiente local. |

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Só logs | Não responde "onde está o atraso" num fluxo assíncrono. |
| SDK de um fornecedor (Application Insights, Datadog) | Prende a instrumentação a um fornecedor; com o OTel, trocar de backend é configuração. |
| Prometheus + Grafana + Jaeger + Loki no compose | Stack completa, mas pesada demais para rodar localmente; o Aspire Dashboard cobre os três sinais num container. |

## Consequências

**Positivas**
- Visibilidade de ponta a ponta do fluxo assíncrono.
- Independência de fornecedor.

**Negativas**
- A propagação de contexto pelo outbox e pela mensagem é manual (concentrada em `RastreamentoDeMensageria`).
- O Aspire Dashboard guarda os dados só em memória: serve para desenvolvimento, não para produção.
