namespace Verity.Consolidado.Core.Dominio.ModelosDeLeitura
{
    public sealed record SaldoConsolidadoDoDia(
        DateOnly Data,
        decimal SaldoInicial,
        decimal TotalCreditos,
        decimal TotalDebitos,
        decimal SaldoFinal,
        DateTime? AtualizadoEmUtc);
}
