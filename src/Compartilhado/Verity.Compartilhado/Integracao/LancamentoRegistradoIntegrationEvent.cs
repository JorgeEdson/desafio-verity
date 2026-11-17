namespace Verity.Compartilhado.Integracao
{
    public sealed record LancamentoRegistradoIntegrationEvent(
        Guid EventoId,
        int Versao,
        DateTime OcorridoEmUtc,
        Guid LancamentoId,
        string ComercianteId,
        string Tipo,
        decimal Valor,
        DateOnly DataCompetencia)
    {
        public const string NomeDoEvento = "LancamentoRegistrado";

        public const int VersaoAtual = 1;

        public const string TipoCredito = "Credito";

        public const string TipoDebito = "Debito";
    }
}
