using Verity.Consolidado.Core.Dominio.Erros;

namespace Verity.Consolidado.Api.Configuracao
{
    public static class StatusHttpDosErrosDoConsolidado
    {
        public static IReadOnlyDictionary<string, int> PorCodigoDeErro { get; } = new Dictionary<string, int>
        {
            [CodigosDeErro.ComercianteNaoIdentificado] = StatusCodes.Status403Forbidden
        };
    }
}
