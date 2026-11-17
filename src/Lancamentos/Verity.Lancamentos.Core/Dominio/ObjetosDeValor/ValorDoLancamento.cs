using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Erros;

namespace Verity.Lancamentos.Core.Dominio.ObjetosDeValor
{
    public sealed class ValorDoLancamento
    {
        public const decimal ValorMaximo = 999_999_999_999.99m;

        private const int CasasDecimaisPermitidas = 2;

        private ValorDoLancamento(decimal valor)
        {
            Valor = valor;
        }

        public decimal Valor { get; }

        public static Resultado<ValorDoLancamento> TentarCriar(decimal valor)
        {
            if (valor <= 0m)
                return Resultado<ValorDoLancamento>.Falha(CodigosDeErro.ValorDoLancamentoDeveSerPositivo);

            if (decimal.Round(valor, CasasDecimaisPermitidas) != valor)
                return Resultado<ValorDoLancamento>.Falha(CodigosDeErro.ValorDoLancamentoComMaisDeDuasCasasDecimais);

            if (valor > ValorMaximo)
                return Resultado<ValorDoLancamento>.Falha(CodigosDeErro.ValorDoLancamentoAcimaDoLimite);

            return Resultado<ValorDoLancamento>.Sucesso(new ValorDoLancamento(valor));
        }
    }
}
