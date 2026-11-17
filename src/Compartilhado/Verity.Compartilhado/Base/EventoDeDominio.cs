namespace Verity.Compartilhado.Base
{
    public abstract class EventoDeDominio
    {
        protected EventoDeDominio(Guid idDoAgregado, DateTime ocorridoEmUtc)
        {
            IdDoEvento = Guid.CreateVersion7();
            IdDoAgregado = idDoAgregado;
            OcorridoEmUtc = ocorridoEmUtc;
        }

        public Guid IdDoEvento { get; }

        public Guid IdDoAgregado { get; }

        public DateTime OcorridoEmUtc { get; }
    }
}
