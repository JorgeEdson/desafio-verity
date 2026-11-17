using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Compartilhado.Infraestrutura.Mensageria;
using Verity.Compartilhado.Integracao;
using Verity.Compartilhado.Observabilidade;
using Verity.Consolidado.Core.Dominio.Enumeradores;
using Verity.Consolidado.Core.UseCases.ConsolidarLancamentoRegistrado;

namespace Verity.Consolidado.Infraestrutura.Mensageria
{
    public sealed class ConsumoDeLancamentosRegistradosBackgroundService(
        IServiceScopeFactory fabricaDeEscopos,
        IOptions<ConexaoRabbitMqOptions> opcoesDaConexao,
        IOptions<ConsumoDeLancamentosRegistradosOptions> opcoes,
        MetricasDaConsolidacao metricas,
        TimeProvider provedorDeTempo,
        ILogger<ConsumoDeLancamentosRegistradosBackgroundService> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await using var conexao = await ConectarComNovasTentativasAsync(stoppingToken);
                await using var canal = await conexao.CreateChannelAsync(cancellationToken: stoppingToken);

                await canal.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: opcoes.Value.QuantidadeMaximaDeMensagensEmProcessamento,
                    global: false,
                    stoppingToken);

                var consumidor = new AsyncEventingBasicConsumer(canal);
                consumidor.ReceivedAsync += (_, entrega) => ProcessarEntregaAsync(canal, entrega, stoppingToken);

                await canal.BasicConsumeAsync(
                    TopologiaDeMensageria.FilaDeLancamentosDoConsolidado,
                    autoAck: false,
                    consumer: consumidor,
                    cancellationToken: stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task<IConnection> ConectarComNovasTentativasAsync(CancellationToken stoppingToken)
        {
            var fabricaDeConexoes = opcoesDaConexao.Value.CriarFabricaDeConexoes();

            while (true)
            {
                try
                {
                    return await fabricaDeConexoes.CreateConnectionAsync(stoppingToken);
                }
                catch (Exception excecao) when (excecao is not OperationCanceledException)
                {
                    logger.LogWarning(
                        excecao,
                        "RabbitMQ indisponível. Nova tentativa de conexão em {Intervalo}.",
                        opcoes.Value.IntervaloEntreTentativasDeConexao);

                    await Task.Delay(opcoes.Value.IntervaloEntreTentativasDeConexao, stoppingToken);
                }
            }
        }

        private async Task ProcessarEntregaAsync(IChannel canal, BasicDeliverEventArgs entrega, CancellationToken stoppingToken)
        {
            using var atividade = RastreamentoDeMensageria.IniciarProcessamento(
                TopologiaDeMensageria.FilaDeLancamentosDoConsolidado,
                entrega.BasicProperties.MessageId,
                entrega.BasicProperties.Headers);

            var inicioDoProcessamento = provedorDeTempo.GetTimestamp();
            var lancamentoRegistrado = TentarLerLancamentoRegistrado(entrega.Body);

            if (lancamentoRegistrado is null)
            {
                logger.LogError("Mensagem {MessageId} com conteúdo ilegível enviada para a fila de rejeitados.", entrega.BasicProperties.MessageId);
                await RejeitarAsync(canal, entrega, atividade, inicioDoProcessamento, "Conteúdo ilegível", stoppingToken);
                return;
            }

            var resultado = await ConsolidarComNovasTentativasAsync(lancamentoRegistrado, stoppingToken);

            if (resultado.EhFalha)
            {
                logger.LogError(
                    "Evento {EventoId} rejeitado por violar o contrato: {Erros}. Enviado para a fila de rejeitados.",
                    lancamentoRegistrado.EventoId,
                    string.Join(", ", resultado.Erros));

                await RejeitarAsync(canal, entrega, atividade, inicioDoProcessamento, "Evento viola o contrato", stoppingToken);
                return;
            }

            await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, stoppingToken);

            metricas.RegistrarEventoAceito(
                resultado.Instancia,
                lancamentoRegistrado.OcorridoEmUtc,
                provedorDeTempo.GetElapsedTime(inicioDoProcessamento));
        }

        private async Task RejeitarAsync(
            IChannel canal,
            BasicDeliverEventArgs entrega,
            Activity? atividade,
            long inicioDoProcessamento,
            string motivoDaRejeicao,
            CancellationToken stoppingToken)
        {
            atividade?.SetStatus(ActivityStatusCode.Error, motivoDaRejeicao);
            await canal.BasicRejectAsync(entrega.DeliveryTag, requeue: false, stoppingToken);
            metricas.RegistrarEventoRejeitado(provedorDeTempo.GetElapsedTime(inicioDoProcessamento));
        }

        private static LancamentoRegistradoIntegrationEvent? TentarLerLancamentoRegistrado(ReadOnlyMemory<byte> conteudo)
        {
            try
            {
                return JsonSerializer.Deserialize<LancamentoRegistradoIntegrationEvent>(
                    conteudo.Span,
                    SerializacaoDeEventosDeIntegracao.Opcoes);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<Resultado<ResultadoDaConsolidacao>> ConsolidarComNovasTentativasAsync(
            LancamentoRegistradoIntegrationEvent lancamentoRegistrado,
            CancellationToken stoppingToken)
        {
            var esperaAteANovaTentativa = opcoes.Value.EsperaInicialAposFalhaTransitoria;

            while (true)
            {
                try
                {
                    await using var escopo = fabricaDeEscopos.CreateAsyncScope();
                    var consolidacao = escopo.ServiceProvider
                        .GetRequiredService<IUseCase<ConsolidarLancamentoRegistradoEntrada, ResultadoDaConsolidacao>>();

                    return await consolidacao.ExecutarAsync(new ConsolidarLancamentoRegistradoEntrada(lancamentoRegistrado), stoppingToken);
                }
                catch (Exception excecao) when (excecao is not OperationCanceledException)
                {
                    logger.LogWarning(
                        excecao,
                        "Falha transitória ao consolidar o evento {EventoId}. Nova tentativa em {Espera}.",
                        lancamentoRegistrado.EventoId,
                        esperaAteANovaTentativa);

                    await Task.Delay(esperaAteANovaTentativa, stoppingToken);
                    esperaAteANovaTentativa = DobrarAteOLimite(esperaAteANovaTentativa);
                }
            }
        }

        private TimeSpan DobrarAteOLimite(TimeSpan espera)
        {
            var esperaDobrada = espera * 2;

            return esperaDobrada <= opcoes.Value.EsperaMaximaAposFalhaTransitoria
                ? esperaDobrada
                : opcoes.Value.EsperaMaximaAposFalhaTransitoria;
        }
    }
}
