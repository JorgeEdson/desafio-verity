using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Verity.Compartilhado.Hospedagem.Respostas
{
    public static class ProblemaDeNegocio
    {
        public const string TipoDeConteudo = "application/problem+json";

        private const string ExtensaoDosCodigosDeErro = "erros";

        public static ProblemDetails Criar(HttpContext contexto, int statusHttp, IReadOnlyCollection<string> codigosDeErro)
        {
            var problema = new ProblemDetails
            {
                Status = statusHttp,
                Title = ReasonPhrases.GetReasonPhrase(statusHttp),
                Instance = contexto.Request.Path
            };

            problema.Extensions[ExtensaoDosCodigosDeErro] = codigosDeErro;

            return problema;
        }
    }
}
