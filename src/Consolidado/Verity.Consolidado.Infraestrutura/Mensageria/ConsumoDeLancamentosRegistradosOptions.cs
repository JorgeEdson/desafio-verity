namespace Verity.Consolidado.Infraestrutura.Mensageria
{
    public sealed class ConsumoDeLancamentosRegistradosOptions
    {
        public const string Secao = "ConsumoDeLancamentosRegistrados";

        public ushort QuantidadeMaximaDeMensagensEmProcessamento { get; set; } = 20;

        public TimeSpan IntervaloEntreTentativasDeConexao { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan EsperaInicialAposFalhaTransitoria { get; set; } = TimeSpan.FromSeconds(1);

        public TimeSpan EsperaMaximaAposFalhaTransitoria { get; set; } = TimeSpan.FromSeconds(30);
    }
}
