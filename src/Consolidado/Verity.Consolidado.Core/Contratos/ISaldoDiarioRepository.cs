using Verity.Consolidado.Core.Dominio.Entidades;

namespace Verity.Consolidado.Core.Contratos
{
    public interface ISaldoDiarioRepository
    {
        Task<SaldoDiario?> ObterUltimoSaldoComMovimentoAteAsync(DateOnly data, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<SaldoDiario>> ListarSaldosComMovimentoNoPeriodoAsync(
            DateOnly inicio,
            DateOnly fim,
            CancellationToken cancellationToken);
    }
}
