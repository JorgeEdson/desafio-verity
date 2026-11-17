using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Erros;

namespace Verity.Lancamentos.Core.Dominio.ObjetosDeValor
{
    public sealed class DataDeCompetencia
    {
        private DataDeCompetencia(DateOnly valor)
        {
            Valor = valor;
        }

        public DateOnly Valor { get; }

        public static Resultado<DataDeCompetencia> TentarCriar(DateOnly data, DateOnly dataDeHojeDoComerciante)
        {
            if (data > dataDeHojeDoComerciante)
                return Resultado<DataDeCompetencia>.Falha(CodigosDeErro.DataDeCompetenciaNoFuturo);

            return Resultado<DataDeCompetencia>.Sucesso(new DataDeCompetencia(data));
        }
    }
}
