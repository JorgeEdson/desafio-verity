namespace Verity.Lancamentos.Infraestrutura.Outbox
{
    public sealed class PublicacaoDoOutboxOptions
    {
        public const string Secao = "Outbox";

        public bool Habilitada { get; set; } = true;

        public int TamanhoDoLote { get; set; } = 50;

        public TimeSpan IntervaloEntreCiclos { get; set; } = TimeSpan.FromSeconds(1);
    }
}
