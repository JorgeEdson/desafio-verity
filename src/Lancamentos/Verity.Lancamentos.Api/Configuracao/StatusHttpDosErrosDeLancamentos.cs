using Verity.Lancamentos.Core.Dominio.Erros;

namespace Verity.Lancamentos.Api.Configuracao
{
    public static class StatusHttpDosErrosDeLancamentos
    {
        public static IReadOnlyDictionary<string, int> PorCodigoDeErro { get; } = new Dictionary<string, int>
        {
            [CodigosDeErro.ComercianteNaoIdentificado] = StatusCodes.Status403Forbidden,
            [CodigosDeErro.LancamentoNaoEncontrado] = StatusCodes.Status404NotFound,
            [CodigosDeErro.LancamentoJaEstornado] = StatusCodes.Status409Conflict,
            [CodigosDeErro.EstornoNaoPodeSerEstornado] = StatusCodes.Status409Conflict,
            [CodigosDeErro.RegistroDuplicado] = StatusCodes.Status409Conflict,
            [CodigosDeErro.ChaveDeIdempotenciaReutilizadaComConteudoDiferente] = StatusCodes.Status422UnprocessableEntity
        };
    }
}
