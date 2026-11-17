using Verity.Consolidado.Core.Dominio.Enumeradores;
using Verity.Consolidado.Testes.Compartilhado;
using Verity.Consolidado.Testes.Integracao.Infraestrutura;

namespace Verity.Consolidado.Testes.Integracao.Consolidacao
{
    [Collection(ColecaoDoAmbienteDoConsolidado.Nome)]
    public sealed class ConsolidacaoDeMovimentosTests(AmbienteDoConsolidadoFixture ambiente)
    {
        private static readonly DateOnly Dia10 = new(2026, 11, 10);
        private static readonly DateOnly Dia11 = new(2026, 11, 11);
        private static readonly DateOnly Dia12 = new(2026, 11, 12);
        private static readonly DateOnly Dia15 = new(2026, 11, 15);

        [Fact]
        public async Task Consolidar_DeveReproduzirOExemploDoEnunciado()
        {
            var comerciante = NovoComerciante();
            var eventos = new LancamentoRegistradoBuilder().DoComerciante(comerciante);

            await ambiente.ConsolidarAsync(eventos.NaData(new DateOnly(2026, 11, 14)).Credito(5000.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(Dia15).Credito(8000.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(Dia15).Debito(1200.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(Dia15).Credito(500.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(Dia15).Debito(2000.00m).Construir());

            var saldoDoDia15 = (await ambiente.ObterSaldosDoComercianteAsync(comerciante))[Dia15];
            Assert.Equal(5000.00m, saldoDoDia15.SaldoInicial);
            Assert.Equal(8500.00m, saldoDoDia15.TotalCreditos);
            Assert.Equal(3200.00m, saldoDoDia15.TotalDebitos);
            Assert.Equal(10300.00m, saldoDoDia15.SaldoFinal);
        }

        [Fact]
        public async Task Consolidar_DeveAplicarOMovimentoUmaUnicaVez_QuandoOMesmoEventoForEntregueTresVezes()
        {
            var comerciante = NovoComerciante();
            var evento = new LancamentoRegistradoBuilder().DoComerciante(comerciante).NaData(Dia10).Credito(300.00m).Construir();

            var primeiraEntrega = await ambiente.ConsolidarAsync(evento);
            var segundaEntrega = await ambiente.ConsolidarAsync(evento);
            var terceiraEntrega = await ambiente.ConsolidarAsync(evento);

            Assert.Equal(ResultadoDaConsolidacao.MovimentoConsolidado, primeiraEntrega);
            Assert.Equal(ResultadoDaConsolidacao.EventoJaConsolidadoAnteriormente, segundaEntrega);
            Assert.Equal(ResultadoDaConsolidacao.EventoJaConsolidadoAnteriormente, terceiraEntrega);
            Assert.Equal(300.00m, (await ambiente.ObterSaldosDoComercianteAsync(comerciante))[Dia10].SaldoFinal);
        }

        [Fact]
        public async Task Consolidar_DeveProduzirOsSaldosCorretos_QuandoEventosChegaremForaDeOrdem()
        {
            var comerciante = NovoComerciante();
            var eventos = new LancamentoRegistradoBuilder().DoComerciante(comerciante);

            await ambiente.ConsolidarAsync(eventos.NaData(Dia10).Credito(100.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(Dia12).Credito(300.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(Dia11).Debito(50.00m).Construir());

            var saldos = await ambiente.ObterSaldosDoComercianteAsync(comerciante);
            Assert.Equal(100.00m, saldos[Dia10].SaldoFinal);
            Assert.Equal(50.00m, saldos[Dia11].SaldoFinal);
            Assert.Equal(350.00m, saldos[Dia12].SaldoFinal);
            Assert.Equal(50.00m, saldos[Dia12].SaldoInicial);
        }

        [Fact]
        public async Task Consolidar_DevePropagarOLancamentoRetroativoParaOsDiasSeguintes_SemAlterarOsAnteriores()
        {
            var comerciante = NovoComerciante();
            var eventos = new LancamentoRegistradoBuilder().DoComerciante(comerciante);
            await ambiente.ConsolidarAsync(eventos.NaData(Dia10).Credito(1000.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(Dia12).Credito(200.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(Dia15).Debito(100.00m).Construir());

            await ambiente.ConsolidarAsync(eventos.NaData(Dia11).Debito(400.00m).Construir());

            var saldos = await ambiente.ObterSaldosDoComercianteAsync(comerciante);
            Assert.Equal(1000.00m, saldos[Dia10].SaldoFinal);
            Assert.Equal(600.00m, saldos[Dia11].SaldoFinal);
            Assert.Equal(800.00m, saldos[Dia12].SaldoFinal);
            Assert.Equal(700.00m, saldos[Dia15].SaldoFinal);
        }

        [Fact]
        public async Task Consolidar_DeveManterOSaldoCorreto_QuandoEventosDoMesmoComercianteForemProcessadosEmParalelo()
        {
            const int QuantidadeDeEventosParalelos = 30;
            var comerciante = NovoComerciante();
            await ambiente.ConsolidarAsync(new LancamentoRegistradoBuilder().DoComerciante(comerciante).NaData(Dia10).Credito(1000.00m).Construir());

            var eventosParalelos = Enumerable.Range(0, QuantidadeDeEventosParalelos)
                .Select(indice => new LancamentoRegistradoBuilder()
                    .DoComerciante(comerciante)
                    .NaData(indice % 2 == 0 ? Dia12 : Dia11)
                    .Credito(10.00m)
                    .Construir());

            await Task.WhenAll(eventosParalelos.Select(ambiente.ConsolidarAsync));

            var saldos = await ambiente.ObterSaldosDoComercianteAsync(comerciante);
            Assert.Equal(1150.00m, saldos[Dia11].SaldoFinal);
            Assert.Equal(1300.00m, saldos[Dia12].SaldoFinal);
            Assert.Equal(1150.00m, saldos[Dia12].SaldoInicial);
        }

        private static string NovoComerciante() => $"c-{Guid.NewGuid():N}";
    }
}
