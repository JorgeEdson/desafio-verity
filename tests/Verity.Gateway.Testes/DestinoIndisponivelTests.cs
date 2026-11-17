using System.Net;
using System.Net.Sockets;
using Verity.Gateway.Testes.Infraestrutura;

namespace Verity.Gateway.Testes
{
    public sealed class DestinoIndisponivelTests
    {
        [Fact]
        public async Task RequisicaoAoServicoDeLancamentos_DeveRetornarBadGateway_QuandoDestinoEstiverForaDoAr()
        {
            var enderecoSemServidorEscutando = $"http://127.0.0.1:{ObterPortaLivre()}/";

            await using var gateway = new GatewayWebApplicationFactory(new Dictionary<string, string?>
            {
                [GatewayWebApplicationFactory.ChaveDoEnderecoDoDestino("lancamentos", "lancamentos1")] = enderecoSemServidorEscutando,
            });
            using var clienteHttp = gateway.CreateClient();

            var resposta = await clienteHttp.GetAsync("/lancamentos/qualquer");

            Assert.Equal(HttpStatusCode.BadGateway, resposta.StatusCode);
        }

        [Fact]
        public async Task Gateway_DeveContinuarSaudavel_QuandoDestinoEstiverForaDoAr()
        {
            var enderecoSemServidorEscutando = $"http://127.0.0.1:{ObterPortaLivre()}/";

            await using var gateway = new GatewayWebApplicationFactory(new Dictionary<string, string?>
            {
                [GatewayWebApplicationFactory.ChaveDoEnderecoDoDestino("lancamentos", "lancamentos1")] = enderecoSemServidorEscutando,
            });
            using var clienteHttp = gateway.CreateClient();

            await clienteHttp.GetAsync("/lancamentos/qualquer");
            var respostaDoHealthCheck = await clienteHttp.GetAsync("/health/live");

            Assert.Equal(HttpStatusCode.OK, respostaDoHealthCheck.StatusCode);
        }

        private static int ObterPortaLivre()
        {
            using var socketTemporario = new TcpListener(IPAddress.Loopback, 0);
            socketTemporario.Start();
            return ((IPEndPoint)socketTemporario.LocalEndpoint).Port;
        }
    }
}
