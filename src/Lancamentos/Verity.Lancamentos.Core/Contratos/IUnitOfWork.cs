using Verity.Compartilhado.Base;

namespace Verity.Lancamentos.Core.Contratos
{
    public interface IUnitOfWork
    {
        ILancamentoRepository Lancamentos { get; }

        Task<Resultado> ConfirmarAlteracoesAsync(CancellationToken cancellationToken);
    }
}
