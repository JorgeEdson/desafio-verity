using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Consolidado.Core.Contratos;
using Verity.Consolidado.Core.Dominio.Erros;
using Verity.Consolidado.Core.Dominio.ModelosDeLeitura;
using Verity.Consolidado.Core.Dominio.Servicos;

namespace Verity.Consolidado.Core.UseCases.ListarSaldosDoPeriodo
{
    public sealed class ListarSaldosDoPeriodoUseCase(
        ISaldoDiarioRepository saldosDiarios,
        IComercianteAtual comercianteAtual)
        : IUseCase<ListarSaldosDoPeriodoEntrada, IReadOnlyList<SaldoConsolidadoDoDia>>
    {
        public const int QuantidadeMaximaDeDiasDoPeriodo = 93;

        public async Task<Resultado<IReadOnlyList<SaldoConsolidadoDoDia>>> ExecutarAsync(
            ListarSaldosDoPeriodoEntrada entrada,
            CancellationToken cancellationToken = default)
        {
            var validacaoDaEntrada = Resultado.Combinar(ValidarComerciante(), ValidarPeriodo(entrada));

            if (validacaoDaEntrada.EhFalha)
                return Resultado<IReadOnlyList<SaldoConsolidadoDoDia>>.Falha(validacaoDaEntrada.Erros);

            var ultimoSaldoComMovimentoAntesDoPeriodo = await saldosDiarios
                .ObterUltimoSaldoComMovimentoAteAsync(entrada.Inicio.AddDays(-1), cancellationToken);

            var saldosComMovimentoNoPeriodo = await saldosDiarios
                .ListarSaldosComMovimentoNoPeriodoAsync(entrada.Inicio, entrada.Fim, cancellationToken);

            return Resultado<IReadOnlyList<SaldoConsolidadoDoDia>>.Sucesso(CalculoDoSaldoConsolidado.CalcularPeriodo(
                entrada.Inicio,
                entrada.Fim,
                ultimoSaldoComMovimentoAntesDoPeriodo,
                saldosComMovimentoNoPeriodo));
        }

        private Resultado ValidarComerciante() =>
            string.IsNullOrWhiteSpace(comercianteAtual.Identificador)
                ? Resultado.Falha(CodigosDeErro.ComercianteNaoIdentificado)
                : Resultado.Sucesso();

        private static Resultado ValidarPeriodo(ListarSaldosDoPeriodoEntrada entrada)
        {
            if (entrada.Fim < entrada.Inicio)
                return Resultado.Falha(CodigosDeErro.PeriodoInvalido);

            var quantidadeDeDiasDoPeriodo = entrada.Fim.DayNumber - entrada.Inicio.DayNumber + 1;

            return quantidadeDeDiasDoPeriodo > QuantidadeMaximaDeDiasDoPeriodo
                ? Resultado.Falha(CodigosDeErro.PeriodoAcimaDoLimite)
                : Resultado.Sucesso();
        }
    }
}
