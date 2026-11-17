using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Erros;

namespace Verity.Lancamentos.Core.Dominio.ObjetosDeValor
{
    public sealed class IdentificadorDoComerciante
    {
        public const int TamanhoMaximo = 64;

        private IdentificadorDoComerciante(string valor)
        {
            Valor = valor;
        }

        public string Valor { get; }

        public static Resultado<IdentificadorDoComerciante> TentarCriar(string? identificador)
        {
            var identificadorSemEspacos = identificador?.Trim() ?? string.Empty;

            if (identificadorSemEspacos.Length == 0)
                return Resultado<IdentificadorDoComerciante>.Falha(CodigosDeErro.ComercianteNaoIdentificado);

            if (identificadorSemEspacos.Length > TamanhoMaximo)
                return Resultado<IdentificadorDoComerciante>.Falha(CodigosDeErro.IdentificadorDoComercianteAcimaDoTamanhoMaximo);

            return Resultado<IdentificadorDoComerciante>.Sucesso(new IdentificadorDoComerciante(identificadorSemEspacos));
        }
    }
}
