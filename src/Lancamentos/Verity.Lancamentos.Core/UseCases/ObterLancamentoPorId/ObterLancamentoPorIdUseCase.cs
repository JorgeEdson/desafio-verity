using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.UseCases.Compartilhado;

namespace Verity.Lancamentos.Core.UseCases.ObterLancamentoPorId
{
    public sealed class ObterLancamentoPorIdUseCase(IUnitOfWork unitOfWork)
        : IUseCase<ObterLancamentoPorIdEntrada, LancamentoSaida>
    {
        public async Task<Resultado<LancamentoSaida>> ExecutarAsync(
            ObterLancamentoPorIdEntrada entrada,
            CancellationToken cancellationToken = default)
        {
            var lancamento = await unitOfWork.Lancamentos.ObterPorIdAsync(entrada.Id, cancellationToken);

            return lancamento is null
                ? Resultado<LancamentoSaida>.Falha(CodigosDeErro.LancamentoNaoEncontrado)
                : Resultado<LancamentoSaida>.Sucesso(LancamentoSaida.APartirDe(lancamento));
        }
    }
}
