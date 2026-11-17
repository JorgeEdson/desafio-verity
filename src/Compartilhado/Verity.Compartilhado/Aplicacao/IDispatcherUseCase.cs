using Verity.Compartilhado.Base;

namespace Verity.Compartilhado.Aplicacao
{
    public interface IDispatcherUseCase
    {
        Task<Resultado<TResposta>> EnviarAsync<TResposta>(
            IRequisicao<TResposta> requisicao,
            CancellationToken cancellationToken = default);
    }
}
