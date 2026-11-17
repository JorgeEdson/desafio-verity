using Verity.Compartilhado.Base;

namespace Verity.Compartilhado.Aplicacao
{
    public sealed class DispatcherUseCase(IServiceProvider provedorDeServicos) : IDispatcherUseCase
    {
        public Task<Resultado<TResposta>> EnviarAsync<TResposta>(
            IRequisicao<TResposta> requisicao,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(requisicao);

            var tipoDaRequisicao = requisicao.GetType();
            var tipoDoUseCase = typeof(IUseCase<,>).MakeGenericType(tipoDaRequisicao, typeof(TResposta));

            var useCase = provedorDeServicos.GetService(tipoDoUseCase)
                ?? throw new InvalidOperationException(
                    $"Nenhum use case registrado para a requisição '{tipoDaRequisicao.Name}' com resposta '{typeof(TResposta).Name}'.");

            var metodoDeExecucao = tipoDoUseCase.GetMethod(nameof(IUseCase<IRequisicao<TResposta>, TResposta>.ExecutarAsync))
                ?? throw new InvalidOperationException($"O método de execução não foi encontrado em '{tipoDoUseCase.Name}'.");

            return (Task<Resultado<TResposta>>)metodoDeExecucao.Invoke(useCase, [requisicao, cancellationToken])!;
        }
    }
}
