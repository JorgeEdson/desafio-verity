namespace Verity.Gateway.Configuracao
{
    public static class ServidorKestrelExtensions
    {
        private const string ChaveDoTamanhoMaximoDoCorpoDaRequisicao = "Gateway:TamanhoMaximoDoCorpoEmBytes";
        private const long TamanhoMaximoPadraoDoCorpoDaRequisicaoEmBytes = 64 * 1024;

        public static IWebHostBuilder ConfigurarServidorKestrel(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureKestrel((contexto, kestrel) =>
            {
                kestrel.AddServerHeader = false;
                kestrel.Limits.MaxRequestBodySize = contexto.Configuration.GetValue(
                    ChaveDoTamanhoMaximoDoCorpoDaRequisicao,
                    TamanhoMaximoPadraoDoCorpoDaRequisicaoEmBytes);
            });
        }
    }
}
