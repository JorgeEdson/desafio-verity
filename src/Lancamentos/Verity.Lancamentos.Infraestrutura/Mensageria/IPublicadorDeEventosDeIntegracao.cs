using Verity.Lancamentos.Infraestrutura.Outbox;

namespace Verity.Lancamentos.Infraestrutura.Mensageria
{
    public interface IPublicadorDeEventosDeIntegracao
    {
        Task PublicarAsync(EventoNoOutbox evento, CancellationToken cancellationToken);
    }
}
