using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Verity.Lancamentos.Infraestrutura.Mensageria;
using Verity.Lancamentos.Infraestrutura.Persistencia;

namespace Verity.Lancamentos.Infraestrutura.Outbox
{
    public sealed class PublicacaoDeEventosPendentes(
        LancamentosDbContext contexto,
        IPublicadorDeEventosDeIntegracao publicador,
        TimeProvider provedorDeTempo,
        ILogger<PublicacaoDeEventosPendentes> logger)
    {
        public async Task<int> PublicarLoteAsync(int tamanhoDoLote, CancellationToken cancellationToken)
        {
            await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

            var eventosPendentes = await contexto.EventosNoOutbox
                .FromSql($"SELECT * FROM outbox WHERE publicado_em_utc IS NULL ORDER BY id LIMIT {tamanhoDoLote} FOR UPDATE SKIP LOCKED")
                .ToListAsync(cancellationToken);

            var quantidadeDeEventosPublicados = 0;

            foreach (var eventoPendente in eventosPendentes)
            {
                var eventoFoiPublicado = await TentarPublicarAsync(eventoPendente, cancellationToken);

                if (!eventoFoiPublicado)
                    break;

                quantidadeDeEventosPublicados++;
            }

            await contexto.SaveChangesAsync(cancellationToken);
            await transacao.CommitAsync(cancellationToken);

            return quantidadeDeEventosPublicados;
        }

        public async Task<SituacaoDoOutbox> ObterSituacaoAsync(CancellationToken cancellationToken)
        {
            var eventosPendentes = contexto.EventosNoOutbox
                .AsNoTracking()
                .Where(evento => evento.PublicadoEmUtc == null);

            return new SituacaoDoOutbox(
                await eventosPendentes.CountAsync(cancellationToken),
                await eventosPendentes.MinAsync(evento => (DateTime?)evento.OcorridoEmUtc, cancellationToken));
        }

        private async Task<bool> TentarPublicarAsync(EventoNoOutbox eventoPendente, CancellationToken cancellationToken)
        {
            try
            {
                await publicador.PublicarAsync(eventoPendente, cancellationToken);
                eventoPendente.MarcarComoPublicado(provedorDeTempo.GetUtcNow().UtcDateTime);
                return true;
            }
            catch (Exception excecao) when (excecao is not OperationCanceledException)
            {
                eventoPendente.RegistrarFalhaDePublicacao(excecao.Message);

                logger.LogWarning(
                    excecao,
                    "Falha ao publicar o evento {EventoId} do outbox (tentativa {Tentativas}). O evento permanece pendente.",
                    eventoPendente.EventoId,
                    eventoPendente.Tentativas);

                return false;
            }
        }
    }
}
