using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Enumeradores;
using Verity.Lancamentos.Core.Dominio.Erros;

namespace Verity.Lancamentos.Core.Dominio.ObjetosDeValor
{
    public static class TiposDeLancamento
    {
        public static Resultado<TipoDeLancamento> TentarConverter(string? tipoInformado)
        {
            var tipoInformadoComoNumero = int.TryParse(tipoInformado, out _);

            if (!tipoInformadoComoNumero
                && Enum.TryParse<TipoDeLancamento>(tipoInformado, ignoreCase: true, out var tipo)
                && Enum.IsDefined(tipo))
            {
                return Resultado<TipoDeLancamento>.Sucesso(tipo);
            }

            return Resultado<TipoDeLancamento>.Falha(CodigosDeErro.TipoDeLancamentoInvalido);
        }

        public static TipoDeLancamento Inverso(this TipoDeLancamento tipo) =>
            tipo == TipoDeLancamento.Credito ? TipoDeLancamento.Debito : TipoDeLancamento.Credito;
    }
}
