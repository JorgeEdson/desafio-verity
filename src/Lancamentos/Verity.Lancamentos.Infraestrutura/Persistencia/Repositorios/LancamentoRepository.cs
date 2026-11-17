using Microsoft.EntityFrameworkCore;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Entidades;

namespace Verity.Lancamentos.Infraestrutura.Persistencia.Repositorios
{
    public sealed class LancamentoRepository(LancamentosDbContext contexto) : ILancamentoRepository
    {
        public Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
            contexto.Lancamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(lancamento => lancamento.Id == id, cancellationToken);

        public Task<Lancamento?> ObterPorChaveDeIdempotenciaAsync(string chaveDeIdempotencia, CancellationToken cancellationToken) =>
            contexto.Lancamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(lancamento => lancamento.ChaveDeIdempotencia == chaveDeIdempotencia, cancellationToken);

        public Task<bool> ExisteEstornoDoLancamentoAsync(Guid idDoLancamento, CancellationToken cancellationToken) =>
            contexto.Lancamentos
                .AnyAsync(lancamento => lancamento.EstornoDeId == idDoLancamento, cancellationToken);

        public async Task<ResultadoPaginado<Lancamento>> ListarPorPeriodoDeCompetenciaAsync(
            DateOnly inicio,
            DateOnly fim,
            int pagina,
            int tamanhoDaPagina,
            CancellationToken cancellationToken)
        {
            var lancamentosDoPeriodo = contexto.Lancamentos
                .AsNoTracking()
                .Where(lancamento => lancamento.DataCompetencia >= inicio && lancamento.DataCompetencia <= fim);

            var totalDeLancamentos = await lancamentosDoPeriodo.CountAsync(cancellationToken);

            var lancamentosDaPagina = await lancamentosDoPeriodo
                .OrderBy(lancamento => lancamento.DataCompetencia)
                .ThenBy(lancamento => lancamento.RegistradoEmUtc)
                .ThenBy(lancamento => lancamento.Id)
                .Skip((pagina - 1) * tamanhoDaPagina)
                .Take(tamanhoDaPagina)
                .ToListAsync(cancellationToken);

            return new ResultadoPaginado<Lancamento>(lancamentosDaPagina, pagina, tamanhoDaPagina, totalDeLancamentos);
        }

        public void Adicionar(Lancamento lancamento) => contexto.Lancamentos.Add(lancamento);
    }
}
