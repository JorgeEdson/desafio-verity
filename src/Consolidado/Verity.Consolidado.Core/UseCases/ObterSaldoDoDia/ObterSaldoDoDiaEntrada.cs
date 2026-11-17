using Verity.Compartilhado.Aplicacao;
using Verity.Consolidado.Core.Dominio.ModelosDeLeitura;

namespace Verity.Consolidado.Core.UseCases.ObterSaldoDoDia
{
    public sealed record ObterSaldoDoDiaEntrada(DateOnly Data) : IRequisicao<SaldoConsolidadoDoDia>;
}
