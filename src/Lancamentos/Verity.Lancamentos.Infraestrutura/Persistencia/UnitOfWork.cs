using Microsoft.EntityFrameworkCore;
using Npgsql;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Erros;

namespace Verity.Lancamentos.Infraestrutura.Persistencia
{
    public sealed class UnitOfWork(LancamentosDbContext contexto, ILancamentoRepository lancamentos) : IUnitOfWork
    {
        public ILancamentoRepository Lancamentos { get; } = lancamentos;

        public async Task<Resultado> ConfirmarAlteracoesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await contexto.SaveChangesAsync(cancellationToken);
                return Resultado.Sucesso();
            }
            catch (DbUpdateException excecao) when (excecao.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violacaoDeUnicidade)
            {
                contexto.ChangeTracker.Clear();
                return Resultado.Falha(TraduzirViolacaoDeUnicidade(violacaoDeUnicidade.ConstraintName));
            }
        }

        private static string TraduzirViolacaoDeUnicidade(string? nomeDaRestricaoViolada) => nomeDaRestricaoViolada switch
        {
            NomesDeRestricoesDoBanco.ChaveDeIdempotenciaUnicaPorComerciante => CodigosDeErro.ChaveDeIdempotenciaJaUtilizada,
            NomesDeRestricoesDoBanco.EstornoUnicoPorLancamento => CodigosDeErro.LancamentoJaEstornado,
            _ => CodigosDeErro.RegistroDuplicado
        };
    }
}
