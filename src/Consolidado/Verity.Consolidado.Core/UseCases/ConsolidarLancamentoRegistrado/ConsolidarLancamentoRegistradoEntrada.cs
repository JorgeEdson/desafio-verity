using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Integracao;
using Verity.Consolidado.Core.Dominio.Enumeradores;

namespace Verity.Consolidado.Core.UseCases.ConsolidarLancamentoRegistrado
{
    public sealed record ConsolidarLancamentoRegistradoEntrada(LancamentoRegistradoIntegrationEvent LancamentoRegistrado)
        : IRequisicao<ResultadoDaConsolidacao>;
}
