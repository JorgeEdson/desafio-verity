using Verity.Consolidado.Core.Dominio.Entidades;
using Verity.Consolidado.Core.Dominio.ModelosDeLeitura;

namespace Verity.Consolidado.Core.Dominio.Servicos
{
    public static class CalculoDoSaldoConsolidado
    {
        public static SaldoConsolidadoDoDia CalcularDia(DateOnly data, SaldoDiario? ultimoSaldoComMovimentoAteAData)
        {
            if (ultimoSaldoComMovimentoAteAData is null)
                return new SaldoConsolidadoDoDia(data, 0m, 0m, 0m, 0m, AtualizadoEmUtc: null);

            if (ultimoSaldoComMovimentoAteAData.Data == data)
            {
                return new SaldoConsolidadoDoDia(
                    data,
                    ultimoSaldoComMovimentoAteAData.SaldoInicial,
                    ultimoSaldoComMovimentoAteAData.TotalCreditos,
                    ultimoSaldoComMovimentoAteAData.TotalDebitos,
                    ultimoSaldoComMovimentoAteAData.SaldoFinal,
                    ultimoSaldoComMovimentoAteAData.AtualizadoEmUtc);
            }

            return new SaldoConsolidadoDoDia(
                data,
                SaldoInicial: ultimoSaldoComMovimentoAteAData.SaldoFinal,
                TotalCreditos: 0m,
                TotalDebitos: 0m,
                SaldoFinal: ultimoSaldoComMovimentoAteAData.SaldoFinal,
                ultimoSaldoComMovimentoAteAData.AtualizadoEmUtc);
        }

        public static IReadOnlyList<SaldoConsolidadoDoDia> CalcularPeriodo(
            DateOnly inicio,
            DateOnly fim,
            SaldoDiario? ultimoSaldoComMovimentoAntesDoPeriodo,
            IReadOnlyCollection<SaldoDiario> saldosComMovimentoNoPeriodo)
        {
            var saldosComMovimentoPorData = saldosComMovimentoNoPeriodo.ToDictionary(saldo => saldo.Data);
            var ultimoSaldoComMovimentoConhecido = ultimoSaldoComMovimentoAntesDoPeriodo;
            var saldosDoPeriodo = new List<SaldoConsolidadoDoDia>();

            for (var data = inicio; data <= fim; data = data.AddDays(1))
            {
                if (saldosComMovimentoPorData.TryGetValue(data, out var saldoComMovimentoNoDia))
                    ultimoSaldoComMovimentoConhecido = saldoComMovimentoNoDia;

                saldosDoPeriodo.Add(CalcularDia(data, ultimoSaldoComMovimentoConhecido));
            }

            return saldosDoPeriodo;
        }
    }
}
