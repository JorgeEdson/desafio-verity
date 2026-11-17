using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.UseCases.Compartilhado;

namespace Verity.Lancamentos.Core.UseCases.ListarLancamentosDoPeriodo
{
    public sealed record ListarLancamentosDoPeriodoEntrada(
        DateOnly Inicio,
        DateOnly Fim,
        int Pagina,
        int TamanhoDaPagina) : IRequisicao<ResultadoPaginado<LancamentoSaida>>;
}
