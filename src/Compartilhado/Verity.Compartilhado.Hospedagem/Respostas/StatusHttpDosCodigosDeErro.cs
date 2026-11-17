using Microsoft.AspNetCore.Http;

namespace Verity.Compartilhado.Hospedagem.Respostas
{
    public sealed class StatusHttpDosCodigosDeErro(IReadOnlyDictionary<string, int> statusHttpPorCodigoDeErro)
    {
        public int Obter(IEnumerable<string> codigosDeErro) =>
            codigosDeErro
                .Where(statusHttpPorCodigoDeErro.ContainsKey)
                .Select(codigoDeErro => statusHttpPorCodigoDeErro[codigoDeErro])
                .DefaultIfEmpty(StatusCodes.Status400BadRequest)
                .First();
    }
}
