namespace Verity.Compartilhado.Integracao
{
    public static class TopologiaDeMensageria
    {
        public const string ExchangeDeLancamentos = "verity.lancamentos";

        public const string ExchangeDeLancamentosRejeitados = "verity.lancamentos.dlx";

        public const string ChaveDeRoteamentoDeLancamentoRegistrado = "lancamento.registrado";

        public const string FilaDeLancamentosDoConsolidado = "consolidado.lancamentos";

        public const string FilaDeLancamentosRejeitadosDoConsolidado = "consolidado.lancamentos.dlq";
    }
}
