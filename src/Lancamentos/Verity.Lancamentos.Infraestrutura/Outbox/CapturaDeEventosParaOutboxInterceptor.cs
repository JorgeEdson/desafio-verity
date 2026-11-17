using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Verity.Compartilhado.Base;

namespace Verity.Lancamentos.Infraestrutura.Outbox
{
    public sealed class CapturaDeEventosParaOutboxInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData dadosDoEvento,
            InterceptionResult<int> resultado)
        {
            TransferirEventosDeDominioParaOOutbox(dadosDoEvento.Context);
            return base.SavingChanges(dadosDoEvento, resultado);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData dadosDoEvento,
            InterceptionResult<int> resultado,
            CancellationToken cancellationToken = default)
        {
            TransferirEventosDeDominioParaOOutbox(dadosDoEvento.Context);
            return base.SavingChangesAsync(dadosDoEvento, resultado, cancellationToken);
        }

        private static void TransferirEventosDeDominioParaOOutbox(DbContext? contexto)
        {
            if (contexto is null)
                return;

            var agregadosComEventosDeDominio = contexto.ChangeTracker
                .Entries<IPossuiEventosDeDominio>()
                .Select(entrada => entrada.Entity)
                .Where(agregado => agregado.EventosDeDominio.Count > 0)
                .ToList();

            foreach (var agregado in agregadosComEventosDeDominio)
            {
                foreach (var eventoDeDominio in agregado.EventosDeDominio)
                    contexto.Add(TradutorDeEventosDeDominioParaIntegracao.Traduzir(eventoDeDominio));

                agregado.LimparEventosDeDominio();
            }
        }
    }
}
