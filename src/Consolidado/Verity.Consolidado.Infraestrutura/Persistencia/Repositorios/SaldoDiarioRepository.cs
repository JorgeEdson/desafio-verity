using Microsoft.EntityFrameworkCore;
using Verity.Consolidado.Core.Contratos;
using Verity.Consolidado.Core.Dominio.Entidades;

namespace Verity.Consolidado.Infraestrutura.Persistencia.Repositorios
{
    public sealed class SaldoDiarioRepository(ConsolidadoDbContext contexto) : ISaldoDiarioRepository
    {
        public Task<SaldoDiario?> ObterUltimoSaldoComMovimentoAteAsync(DateOnly data, CancellationToken cancellationToken) =>
            contexto.SaldosDiarios
                .AsNoTracking()
                .Where(saldo => saldo.Data <= data)
                .OrderByDescending(saldo => saldo.Data)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IReadOnlyCollection<SaldoDiario>> ListarSaldosComMovimentoNoPeriodoAsync(
            DateOnly inicio,
            DateOnly fim,
            CancellationToken cancellationToken) =>
            await contexto.SaldosDiarios
                .AsNoTracking()
                .Where(saldo => saldo.Data >= inicio && saldo.Data <= fim)
                .OrderBy(saldo => saldo.Data)
                .ToListAsync(cancellationToken);
    }
}
