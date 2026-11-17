using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Verity.Compartilhado.Aplicacao;

namespace Verity.Compartilhado.Seguranca
{
    public static class SegurancaDasApisExtensions
    {
        public static IServiceCollection AdicionarAutenticacaoPorJwt(
            this IServiceCollection servicos,
            IConfiguration configuracao)
        {
            servicos.AddOptions<AutenticacaoOptions>().Bind(configuracao.GetSection(AutenticacaoOptions.Secao));

            servicos.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

            servicos.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<AutenticacaoOptions>>((opcoesDoJwtBearer, opcoesDeAutenticacao) =>
                    AplicarValidacaoDoToken(opcoesDoJwtBearer, opcoesDeAutenticacao.Value));

            return servicos;
        }

        public static IServiceCollection AdicionarAutorizacaoPorComerciante(this IServiceCollection servicos)
        {
            servicos.AddAuthorizationBuilder()
                .AddPolicy(PoliticasDeAutorizacao.RequerComerciante, politica => politica
                    .RequireAuthenticatedUser()
                    .RequireClaim(ClaimsDoToken.IdentificadorDoComerciante));

            servicos.AddHttpContextAccessor();
            servicos.AddScoped<IComercianteAtual, ComercianteAtualDaRequisicao>();

            return servicos;
        }

        private static void AplicarValidacaoDoToken(JwtBearerOptions opcoesDoJwtBearer, AutenticacaoOptions autenticacao)
        {
            opcoesDoJwtBearer.MetadataAddress = autenticacao.EnderecoDeMetadados;
            opcoesDoJwtBearer.RequireHttpsMetadata = autenticacao.ExigirHttpsNosMetadados;
            opcoesDoJwtBearer.MapInboundClaims = false;
            opcoesDoJwtBearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = autenticacao.Emissor,
                ValidateAudience = true,
                ValidAudience = autenticacao.Audiencia,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };
        }
    }
}
