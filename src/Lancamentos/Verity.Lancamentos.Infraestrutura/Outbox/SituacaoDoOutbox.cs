namespace Verity.Lancamentos.Infraestrutura.Outbox
{
    public sealed record SituacaoDoOutbox(int EventosPendentes, DateTime? OcorrenciaDoEventoPendenteMaisAntigoEmUtc);
}
