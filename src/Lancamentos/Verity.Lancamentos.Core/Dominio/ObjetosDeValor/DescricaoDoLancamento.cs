using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Erros;

namespace Verity.Lancamentos.Core.Dominio.ObjetosDeValor
{
    public sealed class DescricaoDoLancamento
    {
        public const int TamanhoMaximo = 200;

        private DescricaoDoLancamento(string valor)
        {
            Valor = valor;
        }

        public string Valor { get; }

        public static Resultado<DescricaoDoLancamento> TentarCriar(string? descricao)
        {
            var descricaoSemEspacosNasPontas = descricao?.Trim() ?? string.Empty;

            if (descricaoSemEspacosNasPontas.Length == 0)
                return Resultado<DescricaoDoLancamento>.Falha(CodigosDeErro.DescricaoObrigatoria);

            if (descricaoSemEspacosNasPontas.Length > TamanhoMaximo)
                return Resultado<DescricaoDoLancamento>.Falha(CodigosDeErro.DescricaoAcimaDoTamanhoMaximo);

            return Resultado<DescricaoDoLancamento>.Sucesso(new DescricaoDoLancamento(descricaoSemEspacosNasPontas));
        }
    }
}
