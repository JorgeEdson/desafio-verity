namespace Verity.Compartilhado.Base
{
    public interface IPossuiEventosDeDominio
    {
        IReadOnlyCollection<EventoDeDominio> EventosDeDominio { get; }

        void LimparEventosDeDominio();
    }
}
