using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Entidades;

namespace Verity.Lancamentos.Core.Contratos
{
    public interface ILancamentoRepository
    {
        Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

        Task<Lancamento?> ObterPorChaveDeIdempotenciaAsync(string chaveDeIdempotencia, CancellationToken cancellationToken);

        Task<bool> ExisteEstornoDoLancamentoAsync(Guid idDoLancamento, CancellationToken cancellationToken);

        Task<ResultadoPaginado<Lancamento>> ListarPorPeriodoDeCompetenciaAsync(
            DateOnly inicio,
            DateOnly fim,
            int pagina,
            int tamanhoDaPagina,
            CancellationToken cancellationToken);

        void Adicionar(Lancamento lancamento);
    }
}
