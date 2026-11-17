using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.UseCases.Compartilhado;

namespace Verity.Lancamentos.Core.UseCases.ListarLancamentosDoPeriodo
{
    public sealed class ListarLancamentosDoPeriodoUseCase(IUnitOfWork unitOfWork)
        : IUseCase<ListarLancamentosDoPeriodoEntrada, ResultadoPaginado<LancamentoSaida>>
    {
        public const int QuantidadeMaximaDeDiasDoPeriodo = 93;
        public const int TamanhoMaximoDaPagina = 100;

        public async Task<Resultado<ResultadoPaginado<LancamentoSaida>>> ExecutarAsync(
            ListarLancamentosDoPeriodoEntrada entrada,
            CancellationToken cancellationToken = default)
        {
            var validacaoDaEntrada = Resultado.Combinar(ValidarPeriodo(entrada), ValidarPaginacao(entrada));

            if (validacaoDaEntrada.EhFalha)
                return Resultado<ResultadoPaginado<LancamentoSaida>>.Falha(validacaoDaEntrada.Erros);

            var paginaDeLancamentos = await unitOfWork.Lancamentos.ListarPorPeriodoDeCompetenciaAsync(
                entrada.Inicio,
                entrada.Fim,
                entrada.Pagina,
                entrada.TamanhoDaPagina,
                cancellationToken);

            return Resultado<ResultadoPaginado<LancamentoSaida>>.Sucesso(new ResultadoPaginado<LancamentoSaida>(
                paginaDeLancamentos.Itens.Select(LancamentoSaida.APartirDe).ToArray(),
                paginaDeLancamentos.Pagina,
                paginaDeLancamentos.TamanhoDaPagina,
                paginaDeLancamentos.TotalDeItens));
        }

        private static Resultado ValidarPeriodo(ListarLancamentosDoPeriodoEntrada entrada)
        {
            if (entrada.Fim < entrada.Inicio)
                return Resultado.Falha(CodigosDeErro.PeriodoInvalido);

            var quantidadeDeDiasDoPeriodo = entrada.Fim.DayNumber - entrada.Inicio.DayNumber + 1;

            return quantidadeDeDiasDoPeriodo > QuantidadeMaximaDeDiasDoPeriodo
                ? Resultado.Falha(CodigosDeErro.PeriodoAcimaDoLimite)
                : Resultado.Sucesso();
        }

        private static Resultado ValidarPaginacao(ListarLancamentosDoPeriodoEntrada entrada)
        {
            var paginacaoValida = entrada.Pagina >= 1
                && entrada.TamanhoDaPagina >= 1
                && entrada.TamanhoDaPagina <= TamanhoMaximoDaPagina;

            return paginacaoValida
                ? Resultado.Sucesso()
                : Resultado.Falha(CodigosDeErro.PaginacaoInvalida);
        }
    }
}
