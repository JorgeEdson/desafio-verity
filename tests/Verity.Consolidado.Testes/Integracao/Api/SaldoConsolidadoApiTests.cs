using System.Net;
using System.Net.Http.Json;
using Verity.Consolidado.Core.Dominio.ModelosDeLeitura;
using Verity.Consolidado.Testes.Compartilhado;
using Verity.Consolidado.Testes.Integracao.Infraestrutura;

namespace Verity.Consolidado.Testes.Integracao.Api
{
    [Collection(ColecaoDoAmbienteDoConsolidado.Nome)]
    public sealed class SaldoConsolidadoApiTests(AmbienteDoConsolidadoFixture ambiente)
    {
        [Fact]
        public async Task SaldoDoDia_DeveRetornarOConsolidadoDoDiaComMovimento()
        {
            var comerciante = $"c-{Guid.NewGuid():N}";
            var eventos = new LancamentoRegistradoBuilder().DoComerciante(comerciante);
            await ambiente.ConsolidarAsync(eventos.NaData(new DateOnly(2026, 11, 14)).Credito(5000.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(new DateOnly(2026, 11, 15)).Credito(8500.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(new DateOnly(2026, 11, 15)).Debito(3200.00m).Construir());
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(comerciante);

            var saldo = await clienteHttp.GetFromJsonAsync<SaldoConsolidadoDoDia>("/consolidado/diario/2026-11-15");

            Assert.Equal(5000.00m, saldo!.SaldoInicial);
            Assert.Equal(10300.00m, saldo.SaldoFinal);
        }

        [Fact]
        public async Task SaldoDoDia_DeveHerdarOSaldoDoUltimoDiaComMovimento_QuandoODiaNaoTiverMovimento()
        {
            var comerciante = $"c-{Guid.NewGuid():N}";
            await ambiente.ConsolidarAsync(new LancamentoRegistradoBuilder().DoComerciante(comerciante).NaData(new DateOnly(2026, 11, 1)).Credito(750.00m).Construir());
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(comerciante);

            var saldo = await clienteHttp.GetFromJsonAsync<SaldoConsolidadoDoDia>("/consolidado/diario/2026-11-20");

            Assert.Equal(750.00m, saldo!.SaldoInicial);
            Assert.Equal(0m, saldo.TotalCreditos);
            Assert.Equal(750.00m, saldo.SaldoFinal);
        }

        [Fact]
        public async Task SaldoDoDia_NaoDeveExporOSaldoDeOutroComerciante()
        {
            var comerciante = $"c-{Guid.NewGuid():N}";
            await ambiente.ConsolidarAsync(new LancamentoRegistradoBuilder().DoComerciante(comerciante).NaData(new DateOnly(2026, 11, 5)).Credito(999.00m).Construir());
            using var clienteDeOutroComerciante = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");

            var saldo = await clienteDeOutroComerciante.GetFromJsonAsync<SaldoConsolidadoDoDia>("/consolidado/diario/2026-11-05");

            Assert.Equal(0m, saldo!.SaldoFinal);
        }

        [Fact]
        public async Task Periodo_DeveRetornarUmSaldoPorDia_PreenchendoOsDiasSemMovimento()
        {
            var comerciante = $"c-{Guid.NewGuid():N}";
            var eventos = new LancamentoRegistradoBuilder().DoComerciante(comerciante);
            await ambiente.ConsolidarAsync(eventos.NaData(new DateOnly(2026, 10, 30)).Credito(1000.00m).Construir());
            await ambiente.ConsolidarAsync(eventos.NaData(new DateOnly(2026, 11, 3)).Debito(250.00m).Construir());
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(comerciante);

            var periodo = await clienteHttp.GetFromJsonAsync<List<SaldoConsolidadoDoDia>>("/consolidado/diario?inicio=2026-11-01&fim=2026-11-05");

            Assert.Equal(5, periodo!.Count);
            Assert.Equal(new[] { 1000.00m, 1000.00m, 750.00m, 750.00m, 750.00m }, periodo.Select(dia => dia.SaldoFinal));
        }

        [Fact]
        public async Task Periodo_DeveRetornarBadRequest_QuandoUltrapassarOLimiteDeDias()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");

            var resposta = await clienteHttp.GetAsync("/consolidado/diario?inicio=2026-01-01&fim=2026-12-31");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task Consulta_DeveRetornarUnauthorized_SemToken()
        {
            using var clienteHttp = ambiente.Api.CreateClient();

            var resposta = await clienteHttp.GetAsync("/consolidado/diario/2026-11-15");

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }
    }
}
