using Verity.Consolidado.Core.Dominio.Entidades;
using Verity.Consolidado.Core.Dominio.Servicos;

namespace Verity.Consolidado.Testes.Unidade
{
    public sealed class CalculoDoSaldoConsolidadoTests
    {
        private const string Comerciante = "c-teste";
        private static readonly DateTime InstanteDaAtualizacaoUtc = new(2026, 11, 15, 22, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void CalcularDia_DeveReproduzirOExemploDoEnunciado_QuandoODiaTiverMovimento()
        {
            var saldoDoDia = new SaldoDiario(Comerciante, new DateOnly(2026, 11, 15), 8500.00m, 3200.00m, 10300.00m, InstanteDaAtualizacaoUtc);

            var consolidado = CalculoDoSaldoConsolidado.CalcularDia(new DateOnly(2026, 11, 15), saldoDoDia);

            Assert.Equal(5000.00m, consolidado.SaldoInicial);
            Assert.Equal(8500.00m, consolidado.TotalCreditos);
            Assert.Equal(3200.00m, consolidado.TotalDebitos);
            Assert.Equal(10300.00m, consolidado.SaldoFinal);
        }

        [Fact]
        public void CalcularDia_DeveManterOSaldoDoUltimoDiaComMovimento_QuandoODiaNaoTiverMovimento()
        {
            var ultimoDiaComMovimento = new SaldoDiario(Comerciante, new DateOnly(2026, 11, 10), 500.00m, 0m, 1500.00m, InstanteDaAtualizacaoUtc);

            var consolidado = CalculoDoSaldoConsolidado.CalcularDia(new DateOnly(2026, 11, 14), ultimoDiaComMovimento);

            Assert.Equal(1500.00m, consolidado.SaldoInicial);
            Assert.Equal(0m, consolidado.TotalCreditos);
            Assert.Equal(0m, consolidado.TotalDebitos);
            Assert.Equal(1500.00m, consolidado.SaldoFinal);
        }

        [Fact]
        public void CalcularDia_DeveRetornarSaldoZerado_QuandoNaoHouverNenhumMovimentoAteAData()
        {
            var consolidado = CalculoDoSaldoConsolidado.CalcularDia(new DateOnly(2026, 11, 14), ultimoSaldoComMovimentoAteAData: null);

            Assert.Equal(0m, consolidado.SaldoFinal);
            Assert.Null(consolidado.AtualizadoEmUtc);
        }

        [Fact]
        public void CalcularPeriodo_DevePreencherOsDiasSemMovimentoComOSaldoAnterior()
        {
            var saldoAntesDoPeriodo = new SaldoDiario(Comerciante, new DateOnly(2026, 11, 1), 1000.00m, 0m, 1000.00m, InstanteDaAtualizacaoUtc);
            var saldosNoPeriodo = new[]
            {
                new SaldoDiario(Comerciante, new DateOnly(2026, 11, 3), 200.00m, 50.00m, 1150.00m, InstanteDaAtualizacaoUtc)
            };

            var periodo = CalculoDoSaldoConsolidado.CalcularPeriodo(
                new DateOnly(2026, 11, 2),
                new DateOnly(2026, 11, 4),
                saldoAntesDoPeriodo,
                saldosNoPeriodo);

            Assert.Equal(3, periodo.Count);
            Assert.Equal(new[] { 1000.00m, 1150.00m, 1150.00m }, periodo.Select(dia => dia.SaldoFinal));
            Assert.Equal(new[] { 1000.00m, 1000.00m, 1150.00m }, periodo.Select(dia => dia.SaldoInicial));
        }
    }
}
