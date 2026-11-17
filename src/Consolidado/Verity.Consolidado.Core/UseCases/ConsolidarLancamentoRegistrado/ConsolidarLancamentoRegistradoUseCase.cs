using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Consolidado.Core.Contratos;
using Verity.Consolidado.Core.Dominio.Enumeradores;
using Verity.Consolidado.Core.Dominio.ObjetosDeValor;

namespace Verity.Consolidado.Core.UseCases.ConsolidarLancamentoRegistrado
{
    public sealed class ConsolidarLancamentoRegistradoUseCase(IConsolidacaoDeMovimentos consolidacaoDeMovimentos)
        : IUseCase<ConsolidarLancamentoRegistradoEntrada, ResultadoDaConsolidacao>
    {
        public async Task<Resultado<ResultadoDaConsolidacao>> ExecutarAsync(
            ConsolidarLancamentoRegistradoEntrada entrada,
            CancellationToken cancellationToken = default)
        {
            var movimento = MovimentoDoLancamento.TentarCriar(entrada.LancamentoRegistrado);

            if (movimento.EhFalha)
                return Resultado<ResultadoDaConsolidacao>.Falha(movimento.Erros);

            var resultadoDaConsolidacao = await consolidacaoDeMovimentos.ConsolidarAsync(movimento.Instancia, cancellationToken);

            return Resultado<ResultadoDaConsolidacao>.Sucesso(resultadoDaConsolidacao);
        }
    }
}
