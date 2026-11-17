using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Verity.Compartilhado.Seguranca;
using Verity.Testes.Compartilhado.Autenticacao;

namespace Verity.Testes.Compartilhado.Hospedagem
{
    public abstract class ApiAutenticadaWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        private const string AmbienteDeTestes = "Testes";

        protected abstract IReadOnlyDictionary<string, string?> ConfiguracaoDoServico { get; }

        public HttpClient CriarClienteDoComerciante(string identificadorDoComerciante) =>
            CriarClienteComToken(EmissorDeTokensDeTeste.EmitirTokenDoComerciante(identificadorDoComerciante));

        public HttpClient CriarClienteComToken(string token)
        {
            var clienteHttp = CreateClient();
            clienteHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
            return clienteHttp;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(AmbienteDeTestes);

            builder.ConfigureAppConfiguration((_, configuracao) => configuracao
                .AddInMemoryCollection(ConfiguracaoDaAutenticacaoDeTeste())
                .AddInMemoryCollection(ConfiguracaoDoServico));

            builder.ConfigureTestServices(servicos => servicos.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                opcoesDoJwtBearer =>
                {
                    opcoesDoJwtBearer.ConfigurationManager = null;
                    opcoesDoJwtBearer.Configuration = new OpenIdConnectConfiguration();
                    opcoesDoJwtBearer.TokenValidationParameters.IssuerSigningKey = EmissorDeTokensDeTeste.ChaveDeAssinatura;
                }));
        }

        private static Dictionary<string, string?> ConfiguracaoDaAutenticacaoDeTeste() => new()
        {
            [$"{AutenticacaoOptions.Secao}:{nameof(AutenticacaoOptions.Emissor)}"] = EmissorDeTokensDeTeste.Emissor,
            [$"{AutenticacaoOptions.Secao}:{nameof(AutenticacaoOptions.Audiencia)}"] = EmissorDeTokensDeTeste.Audiencia,
            [$"{AutenticacaoOptions.Secao}:{nameof(AutenticacaoOptions.EnderecoDeMetadados)}"] = EmissorDeTokensDeTeste.EnderecoDeMetadados
        };
    }
}
