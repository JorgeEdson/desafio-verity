using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Verity.Compartilhado.Observabilidade
{
    public static class ObservabilidadeExtensions
    {
        private const string EnderecoDoColetorDeTelemetria = "OTEL_EXPORTER_OTLP_ENDPOINT";
        private const string PrefixoDasRotasDeVerificacaoDeSaude = "/health";
        private const string FontesEMedidoresDaSolucao = "Verity.*";
        private const string FonteEMedidorDoNpgsql = "Npgsql";

        public static TBuilder AdicionarObservabilidade<TBuilder>(this TBuilder builder, string nomeDoServico)
            where TBuilder : IHostApplicationBuilder
        {
            builder.Logging.AddOpenTelemetry(registroDeLogs =>
            {
                registroDeLogs.IncludeFormattedMessage = true;
                registroDeLogs.IncludeScopes = true;
            });

            var openTelemetry = builder.Services.AddOpenTelemetry()
                .ConfigureResource(recurso => recurso.AddService(nomeDoServico, serviceInstanceId: Environment.MachineName))
                .WithTracing(rastreamento => rastreamento
                    .AddAspNetCoreInstrumentation(opcoes => opcoes.Filter = NaoEhVerificacaoDeSaude)
                    .AddHttpClientInstrumentation(opcoes => opcoes.FilterHttpRequestMessage = NaoEhVerificacaoDeSaude)
                    .AddSource(FontesEMedidoresDaSolucao, FonteEMedidorDoNpgsql))
                .WithMetrics(metricas => metricas
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(FontesEMedidoresDaSolucao, FonteEMedidorDoNpgsql));

            if (!string.IsNullOrWhiteSpace(builder.Configuration[EnderecoDoColetorDeTelemetria]))
                openTelemetry.UseOtlpExporter();

            return builder;
        }

        private static bool NaoEhVerificacaoDeSaude(HttpContext contexto) =>
            !contexto.Request.Path.StartsWithSegments(PrefixoDasRotasDeVerificacaoDeSaude);

        private static bool NaoEhVerificacaoDeSaude(HttpRequestMessage requisicao) =>
            requisicao.RequestUri?.AbsolutePath.StartsWith(PrefixoDasRotasDeVerificacaoDeSaude, StringComparison.OrdinalIgnoreCase) != true;
    }
}
