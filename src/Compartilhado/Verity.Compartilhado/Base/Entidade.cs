namespace Verity.Compartilhado.Base
{
    public abstract class Entidade : IPossuiEventosDeDominio
    {
        private readonly List<EventoDeDominio> _eventosDeDominio = [];

        protected Entidade(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }

        public IReadOnlyCollection<EventoDeDominio> EventosDeDominio => _eventosDeDominio.AsReadOnly();

        public void LimparEventosDeDominio() => _eventosDeDominio.Clear();

        protected void AdicionarEventoDeDominio(EventoDeDominio eventoDeDominio) => _eventosDeDominio.Add(eventoDeDominio);
    }
}
