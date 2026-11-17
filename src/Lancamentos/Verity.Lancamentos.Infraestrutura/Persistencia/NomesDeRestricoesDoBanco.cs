namespace Verity.Lancamentos.Infraestrutura.Persistencia
{
    public static class NomesDeRestricoesDoBanco
    {
        public const string ChaveDeIdempotenciaUnicaPorComerciante = "ux_lancamentos_comerciante_chave_de_idempotencia";
        public const string EstornoUnicoPorLancamento = "ux_lancamentos_estorno_de_id";
        public const string LancamentosPorComercianteEDataDeCompetencia = "ix_lancamentos_comerciante_data_competencia";
        public const string EventoUnicoNoOutbox = "ux_outbox_evento_id";
        public const string EventosPendentesNoOutbox = "ix_outbox_pendentes";
    }
}
