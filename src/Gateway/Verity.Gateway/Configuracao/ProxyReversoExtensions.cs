namespace Verity.Gateway.Configuracao
{
    public static class ProxyReversoExtensions
    {
        private const string SecaoDeConfiguracaoDoProxyReverso = "ReverseProxy";

        public static IServiceCollection AdicionarProxyReverso(
            this IServiceCollection servicos,
            IConfiguration configuracao)
        {
            servicos
                .AddReverseProxy()
                .LoadFromConfig(configuracao.GetSection(SecaoDeConfiguracaoDoProxyReverso));

            return servicos;
        }
    }
}
