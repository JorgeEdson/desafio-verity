namespace Verity.Lancamentos.Core.Dominio.Erros
{
    public static class CodigosDeErro
    {
        public const string ComercianteNaoIdentificado = "COMERCIANTE_NAO_IDENTIFICADO";
        public const string IdentificadorDoComercianteAcimaDoTamanhoMaximo = "IDENTIFICADOR_DO_COMERCIANTE_ACIMA_DO_TAMANHO_MAXIMO";
        public const string TipoDeLancamentoInvalido = "TIPO_DE_LANCAMENTO_INVALIDO";
        public const string ValorDoLancamentoDeveSerPositivo = "VALOR_DO_LANCAMENTO_DEVE_SER_POSITIVO";
        public const string ValorDoLancamentoComMaisDeDuasCasasDecimais = "VALOR_DO_LANCAMENTO_COM_MAIS_DE_DUAS_CASAS_DECIMAIS";
        public const string ValorDoLancamentoAcimaDoLimite = "VALOR_DO_LANCAMENTO_ACIMA_DO_LIMITE";
        public const string DataDeCompetenciaNoFuturo = "DATA_DE_COMPETENCIA_NO_FUTURO";
        public const string DescricaoObrigatoria = "DESCRICAO_OBRIGATORIA";
        public const string DescricaoAcimaDoTamanhoMaximo = "DESCRICAO_ACIMA_DO_TAMANHO_MAXIMO";
        public const string ChaveDeIdempotenciaObrigatoria = "CHAVE_DE_IDEMPOTENCIA_OBRIGATORIA";
        public const string ChaveDeIdempotenciaAcimaDoTamanhoMaximo = "CHAVE_DE_IDEMPOTENCIA_ACIMA_DO_TAMANHO_MAXIMO";
        public const string ChaveDeIdempotenciaReutilizadaComConteudoDiferente = "CHAVE_DE_IDEMPOTENCIA_REUTILIZADA_COM_CONTEUDO_DIFERENTE";
        public const string ChaveDeIdempotenciaJaUtilizada = "CHAVE_DE_IDEMPOTENCIA_JA_UTILIZADA";
        public const string LancamentoNaoEncontrado = "LANCAMENTO_NAO_ENCONTRADO";
        public const string LancamentoJaEstornado = "LANCAMENTO_JA_ESTORNADO";
        public const string EstornoNaoPodeSerEstornado = "ESTORNO_NAO_PODE_SER_ESTORNADO";
        public const string RegistroDuplicado = "REGISTRO_DUPLICADO";
        public const string PeriodoInvalido = "PERIODO_INVALIDO";
        public const string PeriodoAcimaDoLimite = "PERIODO_ACIMA_DO_LIMITE";
        public const string PaginacaoInvalida = "PAGINACAO_INVALIDA";
    }
}
