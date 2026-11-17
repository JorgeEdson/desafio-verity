using Verity.Compartilhado.Base;

namespace Verity.Compartilhado.Aplicacao
{
    public interface IUseCase<in TEntrada, TSaida>
        where TEntrada : IRequisicao<TSaida>
    {
        Task<Resultado<TSaida>> ExecutarAsync(TEntrada entrada, CancellationToken cancellationToken = default);
    }
}
