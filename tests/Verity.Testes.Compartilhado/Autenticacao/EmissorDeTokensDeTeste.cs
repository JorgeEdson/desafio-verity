using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Verity.Compartilhado.Seguranca;

namespace Verity.Testes.Compartilhado.Autenticacao
{
    public static class EmissorDeTokensDeTeste
    {
        public const string Emissor = "https://emissor-de-testes.verity.local/realms/verity";
        public const string Audiencia = "verity-api";
        public const string EnderecoDeMetadados = Emissor + "/.well-known/openid-configuration";

        private const string ClaimDoUsuario = "sub";

        public static RsaSecurityKey ChaveDeAssinatura { get; } = new(RSA.Create(2048));

        public static string EmitirTokenDoComerciante(string identificadorDoComerciante) =>
            EmitirToken(new Dictionary<string, object>
            {
                [ClaimDoUsuario] = Guid.NewGuid().ToString(),
                [ClaimsDoToken.IdentificadorDoComerciante] = identificadorDoComerciante
            });

        public static string EmitirTokenSemComerciante() =>
            EmitirToken(new Dictionary<string, object>
            {
                [ClaimDoUsuario] = Guid.NewGuid().ToString()
            });

        private static string EmitirToken(IDictionary<string, object> claims) =>
            new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Emissor,
                Audience = Audiencia,
                Expires = DateTime.UtcNow.AddMinutes(5),
                Claims = claims,
                SigningCredentials = new SigningCredentials(ChaveDeAssinatura, SecurityAlgorithms.RsaSha256)
            });
    }
}
