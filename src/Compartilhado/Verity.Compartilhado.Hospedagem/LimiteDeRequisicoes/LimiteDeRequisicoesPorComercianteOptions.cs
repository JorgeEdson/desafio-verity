namespace Verity.Compartilhado.Hospedagem.LimiteDeRequisicoes
{
    public sealed class LimiteDeRequisicoesPorComercianteOptions
    {
        public const string Secao = "LimiteDeRequisicoesPorComerciante";

        public bool Habilitado { get; set; } = true;

        public int LimiteDeRajada { get; set; } = 200;

        public int RequisicoesPorPeriodo { get; set; } = 100;

        public TimeSpan Periodo { get; set; } = TimeSpan.FromSeconds(1);
    }
}
