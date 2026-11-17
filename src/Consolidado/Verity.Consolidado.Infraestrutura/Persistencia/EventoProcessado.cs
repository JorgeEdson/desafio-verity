namespace Verity.Consolidado.Infraestrutura.Persistencia
{
    public sealed class EventoProcessado
    {
        public EventoProcessado(Guid eventoId, DateTime processadoEmUtc)
        {
            EventoId = eventoId;
            ProcessadoEmUtc = processadoEmUtc;
        }

        public Guid EventoId { get; private set; }

        public DateTime ProcessadoEmUtc { get; private set; }
    }
}
