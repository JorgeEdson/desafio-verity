using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Verity.Gateway.Testes.Infraestrutura;

namespace Verity.Gateway.Testes
{
    public sealed class RoteamentoDoGatewayTests : IAsyncLifetime
    {
        private const int QuantidadeDeRequisicoesParaVerificarDistribuicao = 40;

        private ApiDeDestinoFake _apiDeLancamentos = null!;
        private ApiDeDestinoFake _primeiraReplicaDoConsolidado = null!;
        private ApiDeDestinoFake _segundaReplicaDoConsolidado = null!;
        private GatewayWebApplicationFactory _gateway = null!;
        private HttpClient _clienteHttp = null!;

        public async Task InitializeAsync()
        {
            _apiDeLancamentos = await ApiDeDestinoFake.IniciarAsync("lancamentos");
            _primeiraReplicaDoConsolidado = await ApiDeDestinoFake.IniciarAsync("consolidado1");
            _segundaReplicaDoConsolidado = await ApiDeDestinoFake.IniciarAsync("consolidado2");

            _gateway = new GatewayWebApplicationFactory(new Dictionary<string, string?>
            {
                [GatewayWebApplicationFactory.ChaveDoEnderecoDoDestino("lancamentos", "lancamentos1")] = _apiDeLancamentos.Endereco,
                [GatewayWebApplicationFactory.ChaveDoEnderecoDoDestino("consolidado", "consolidado1")] = _primeiraReplicaDoConsolidado.Endereco,
                [GatewayWebApplicationFactory.ChaveDoEnderecoDoDestino("consolidado", "consolidado2")] = _segundaReplicaDoConsolidado.Endereco,
            });

            _clienteHttp = _gateway.CreateClient();
        }

        public async Task DisposeAsync()
        {
            _clienteHttp.Dispose();
            await _gateway.DisposeAsync();
            await _apiDeLancamentos.DisposeAsync();
            await _primeiraReplicaDoConsolidado.DisposeAsync();
            await _segundaReplicaDoConsolidado.DisposeAsync();
        }

        [Fact]
        public async Task RequisicaoGet_DeveSerEncaminhadaAoServicoDeLancamentos_PreservandoCaminhoEQuery()
        {
            var resposta = await _clienteHttp.GetAsync("/lancamentos/0b6f1c2e?pagina=2");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(_apiDeLancamentos.Nome, await resposta.Content.ReadAsStringAsync());
            Assert.Contains("/lancamentos/0b6f1c2e?pagina=2", _apiDeLancamentos.CaminhosRecebidos);
        }

        [Fact]
        public async Task RequisicaoPostNaRaizDeLancamentos_DeveSerEncaminhadaAoServicoDeLancamentos()
        {
            var resposta = await _clienteHttp.PostAsJsonAsync("/lancamentos", new { tipo = "Credito", valor = 10.00m });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Contains("/lancamentos", _apiDeLancamentos.CaminhosRecebidos);
        }

        [Fact]
        public async Task RequisicoesAoConsolidado_DevemSerDistribuidasEntreAsDuasReplicas()
        {
            for (var requisicao = 0; requisicao < QuantidadeDeRequisicoesParaVerificarDistribuicao; requisicao++)
            {
                var resposta = await _clienteHttp.GetAsync("/consolidado/diario/2026-11-15");
                Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            }

            Assert.NotEmpty(_primeiraReplicaDoConsolidado.CaminhosRecebidos);
            Assert.NotEmpty(_segundaReplicaDoConsolidado.CaminhosRecebidos);
            Assert.Empty(_apiDeLancamentos.CaminhosRecebidos);
        }

        [Fact]
        public async Task CabecalhoAuthorization_DeveSerRepassadoSemAlteracao()
        {
            using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/lancamentos/qualquer");
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token-de-teste");

            await _clienteHttp.SendAsync(requisicao);

            Assert.Contains("Bearer token-de-teste", _apiDeLancamentos.CabecalhosAuthorizationRecebidos);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/admin")]
        [InlineData("/lancamentosx")]
        public async Task CaminhoForaDasRotas_DeveRetornarNotFound(string caminho)
        {
            var resposta = await _clienteHttp.GetAsync(caminho);

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        [Theory]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public async Task HealthChecksDoGateway_DevemRetornarOk(string caminho)
        {
            var resposta = await _clienteHttp.GetAsync(caminho);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }
    }
}
