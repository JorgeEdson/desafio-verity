using Verity.Lancamentos.Core.Contratos;

namespace Verity.Lancamentos.Infraestrutura.Tempo
{
    public sealed class RelogioDoComerciante(TimeProvider provedorDeTempo) : IRelogioDoComerciante
    {
        private const string IdentificadorDoFusoHorarioDoComerciante = "America/Sao_Paulo";

        private static readonly TimeZoneInfo FusoHorarioDoComerciante =
            TimeZoneInfo.FindSystemTimeZoneById(IdentificadorDoFusoHorarioDoComerciante);

        public DateOnly ObterDataDeHoje() =>
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(provedorDeTempo.GetUtcNow(), FusoHorarioDoComerciante).DateTime);

        public DateTime ObterInstanteAtualUtc() => provedorDeTempo.GetUtcNow().UtcDateTime;
    }
}
