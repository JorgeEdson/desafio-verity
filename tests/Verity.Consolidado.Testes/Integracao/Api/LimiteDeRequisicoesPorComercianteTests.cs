using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Verity.Compartilhado.Hospedagem.LimiteDeRequisicoes;
using Verity.Consolidado.Testes.Integracao.Infraestrutura;

namespace Verity.Consolidado.Testes.Integracao.Api
{
    [Collection(ColecaoDoAmbienteDoConsolidado.Nome)]
    public sealed class LimiteDeRequisicoesPorComercianteTests(AmbienteDoConsolidadoFixture ambiente) : IAsyncLifetime
    {
        private const int LimiteDeRajadaDoTeste = 2;
        private const string RotaDoSaldoDoDia = "/consolidado/diario/2026-11-15";

        private ConsolidadoApiWebApplicationFactory _apiComLimiteBaixo = null!;

        public Task InitializeAsync()
        {
            _apiComLimiteBaixo = new ConsolidadoApiWebApplicationFactory(
                ambiente.StringDeConexaoDoBanco,
                new Dictionary<string, string?>
                {
                    [$"{LimiteDeRequisicoesPorComercianteOptions.Secao}:{nameof(LimiteDeRequisicoesPorComercianteOptions.LimiteDeRajada)}"] = LimiteDeRajadaDoTeste.ToString(),
                    [$"{LimiteDeRequisicoesPorComercianteOptions.Secao}:{nameof(LimiteDeRequisicoesPorComercianteOptions.RequisicoesPorPeriodo)}"] = "1",
                    [$"{LimiteDeRequisicoesPorComercianteOptions.Secao}:{nameof(LimiteDeRequisicoesPorComercianteOptions.Periodo)}"] = "00:10:00"
                });

            return Task.CompletedTask;
        }

        public async Task DisposeAsync() => await _apiComLimiteBaixo.DisposeAsync();

        [Fact]
        public async Task RequisicaoAcimaDoLimite_DeveReceber429ComRetryAfterECodigoDeErro()
        {
            using var clienteHttp = _apiComLimiteBaixo.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            await EsgotarLimiteAsync(clienteHttp);

            using var resposta = await clienteHttp.GetAsync(RotaDoSaldoDoDia);
            var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.TooManyRequests, resposta.StatusCode);
            Assert.True(resposta.Headers.RetryAfter is not null);
            Assert.Contains(
                LimiteDeRequisicoesPorComercianteExtensions.CodigoDoErroDeLimiteExcedido,
                problema.GetProperty("erros").EnumerateArray().Select(erro => erro.GetString()));
        }

        [Fact]
        public async Task LimiteEsgotadoPorUmComerciante_NaoDeveAfetarOutroComerciante()
        {
            using var clienteDoComercianteNoLimite = _apiComLimiteBaixo.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            using var clienteDeOutroComerciante = _apiComLimiteBaixo.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            await EsgotarLimiteAsync(clienteDoComercianteNoLimite);

            using var resposta = await clienteDeOutroComerciante.GetAsync(RotaDoSaldoDoDia);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task VerificacoesDeSaude_NaoDevemSerLimitadas()
        {
            using var clienteSemToken = _apiComLimiteBaixo.CreateClient();

            for (var requisicao = 0; requisicao <= LimiteDeRajadaDoTeste * 2; requisicao++)
            {
                using var resposta = await clienteSemToken.GetAsync("/health/live");
                Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            }
        }

        private static async Task EsgotarLimiteAsync(HttpClient clienteHttp)
        {
            for (var requisicao = 0; requisicao < LimiteDeRajadaDoTeste; requisicao++)
            {
                using var resposta = await clienteHttp.GetAsync(RotaDoSaldoDoDia);
                Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            }
        }
    }
}
