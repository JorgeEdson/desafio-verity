namespace Verity.Consolidado.Core.Dominio.Entidades
{
    public sealed class SaldoDiario
    {
        public SaldoDiario(
            string comercianteId,
            DateOnly data,
            decimal totalCreditos,
            decimal totalDebitos,
            decimal saldoFinal,
            DateTime atualizadoEmUtc)
        {
            ComercianteId = comercianteId;
            Data = data;
            TotalCreditos = totalCreditos;
            TotalDebitos = totalDebitos;
            SaldoFinal = saldoFinal;
            AtualizadoEmUtc = atualizadoEmUtc;
        }

        public string ComercianteId { get; private set; }

        public DateOnly Data { get; private set; }

        public decimal TotalCreditos { get; private set; }

        public decimal TotalDebitos { get; private set; }

        public decimal SaldoFinal { get; private set; }

        public DateTime AtualizadoEmUtc { get; private set; }

        public decimal SaldoInicial => SaldoFinal - TotalCreditos + TotalDebitos;
    }
}
