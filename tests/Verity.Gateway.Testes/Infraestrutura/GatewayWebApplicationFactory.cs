using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Verity.Gateway.Testes.Infraestrutura
{
    internal sealed class GatewayWebApplicationFactory(IReadOnlyDictionary<string, string?> enderecosDosDestinos)
        : WebApplicationFactory<Program>
    {
        private const string AmbienteDeTestes = "Testes";

        public static string ChaveDoEnderecoDoDestino(string cluster, string destino) =>
            $"ReverseProxy:Clusters:{cluster}:Destinations:{destino}:Address";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(AmbienteDeTestes);
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(enderecosDosDestinos));
        }
    }
}
