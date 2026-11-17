using Microsoft.AspNetCore.OutputCaching;
using Verity.Compartilhado.Seguranca;

namespace Verity.Consolidado.Api.Seguranca
{
    public sealed class CacheDoSaldoConsolidadoPorComerciantePolicy : IOutputCachePolicy
    {
        private const string ChaveDeVariacaoPorComerciante = "comerciante";
        private const string TodosOsParametrosDeConsulta = "*";

        private static readonly TimeSpan TempoDeVidaDaRespostaEmCache = TimeSpan.FromSeconds(5);

        public ValueTask CacheRequestAsync(OutputCacheContext contexto, CancellationToken cancellationToken)
        {
            var identificadorDoComerciante = contexto.HttpContext.User.FindFirst(ClaimsDoToken.IdentificadorDoComerciante)?.Value;
            var requisicaoPodeUsarCache = HttpMethods.IsGet(contexto.HttpContext.Request.Method)
                && !string.IsNullOrWhiteSpace(identificadorDoComerciante);

            contexto.EnableOutputCaching = true;
            contexto.AllowCacheLookup = requisicaoPodeUsarCache;
            contexto.AllowCacheStorage = requisicaoPodeUsarCache;
            contexto.AllowLocking = true;
            contexto.ResponseExpirationTimeSpan = TempoDeVidaDaRespostaEmCache;
            contexto.CacheVaryByRules.QueryKeys = TodosOsParametrosDeConsulta;
            contexto.CacheVaryByRules.VaryByValues[ChaveDeVariacaoPorComerciante] = identificadorDoComerciante ?? string.Empty;

            return ValueTask.CompletedTask;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext contexto, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask ServeResponseAsync(OutputCacheContext contexto, CancellationToken cancellationToken)
        {
            if (contexto.HttpContext.Response.StatusCode != StatusCodes.Status200OK)
                contexto.AllowCacheStorage = false;

            return ValueTask.CompletedTask;
        }
    }
}
