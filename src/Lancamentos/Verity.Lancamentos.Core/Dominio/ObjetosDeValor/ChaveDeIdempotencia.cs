using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Erros;

namespace Verity.Lancamentos.Core.Dominio.ObjetosDeValor
{
    public sealed class ChaveDeIdempotencia
    {
        public const int TamanhoMaximo = 100;

        private ChaveDeIdempotencia(string valor)
        {
            Valor = valor;
        }

        public string Valor { get; }

        public static Resultado<ChaveDeIdempotencia> TentarCriar(string? chave)
        {
            var chaveSemEspacosNasPontas = chave?.Trim() ?? string.Empty;

            if (chaveSemEspacosNasPontas.Length == 0)
                return Resultado<ChaveDeIdempotencia>.Falha(CodigosDeErro.ChaveDeIdempotenciaObrigatoria);

            if (chaveSemEspacosNasPontas.Length > TamanhoMaximo)
                return Resultado<ChaveDeIdempotencia>.Falha(CodigosDeErro.ChaveDeIdempotenciaAcimaDoTamanhoMaximo);

            return Resultado<ChaveDeIdempotencia>.Sucesso(new ChaveDeIdempotencia(chaveSemEspacosNasPontas));
        }
    }
}
