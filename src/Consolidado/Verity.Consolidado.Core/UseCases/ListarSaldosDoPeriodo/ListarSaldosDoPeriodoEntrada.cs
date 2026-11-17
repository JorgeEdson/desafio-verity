using Verity.Compartilhado.Aplicacao;
using Verity.Consolidado.Core.Dominio.ModelosDeLeitura;

namespace Verity.Consolidado.Core.UseCases.ListarSaldosDoPeriodo
{
    public sealed record ListarSaldosDoPeriodoEntrada(DateOnly Inicio, DateOnly Fim)
        : IRequisicao<IReadOnlyList<SaldoConsolidadoDoDia>>;
}
