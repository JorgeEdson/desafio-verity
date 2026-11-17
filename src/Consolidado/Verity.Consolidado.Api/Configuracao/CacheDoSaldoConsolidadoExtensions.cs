using Verity.Consolidado.Api.Seguranca;

namespace Verity.Consolidado.Api.Configuracao
{
    public static class CacheDoSaldoConsolidadoExtensions
    {
        public static IServiceCollection AdicionarCacheDoSaldoConsolidado(this IServiceCollection servicos)
        {
            servicos.AddOutputCache(opcoes => opcoes.AddPolicy(
                PoliticasDeCache.SaldoConsolidadoPorComerciante,
                new CacheDoSaldoConsolidadoPorComerciantePolicy()));

            return servicos;
        }
    }
}
