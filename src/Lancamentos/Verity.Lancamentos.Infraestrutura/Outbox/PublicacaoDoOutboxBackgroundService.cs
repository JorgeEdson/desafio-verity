using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Verity.Lancamentos.Infraestrutura.Outbox
{
    public sealed class PublicacaoDoOutboxBackgroundService(
        IServiceScopeFactory fabricaDeEscopos,
        IOptions<PublicacaoDoOutboxOptions> opcoes,
        MetricasDoOutbox metricas,
        ILogger<PublicacaoDoOutboxBackgroundService> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!opcoes.Value.Habilitada)
                return;

            while (!stoppingToken.IsCancellationRequested)
            {
                var quantidadeDeEventosPublicados = await PublicarLoteSemInterromperOServicoAsync(stoppingToken);
                var loteVeioCheio = quantidadeDeEventosPublicados >= opcoes.Value.TamanhoDoLote;

                if (!loteVeioCheio)
                    await AguardarProximoCicloAsync(stoppingToken);
            }
        }

        private async Task<int> PublicarLoteSemInterromperOServicoAsync(CancellationToken stoppingToken)
        {
            try
            {
                await using var escopo = fabricaDeEscopos.CreateAsyncScope();
                var publicacao = escopo.ServiceProvider.GetRequiredService<PublicacaoDeEventosPendentes>();
                var quantidadeDeEventosPublicados = await publicacao.PublicarLoteAsync(opcoes.Value.TamanhoDoLote, stoppingToken);

                metricas.RegistrarEventosPublicados(quantidadeDeEventosPublicados);
                metricas.AtualizarSituacao(await publicacao.ObterSituacaoAsync(stoppingToken));

                return quantidadeDeEventosPublicados;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return 0;
            }
            catch (Exception excecao)
            {
                logger.LogError(excecao, "Falha ao processar o lote de eventos pendentes do outbox.");
                return 0;
            }
        }

        private async Task AguardarProximoCicloAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(opcoes.Value.IntervaloEntreCiclos, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }
}
