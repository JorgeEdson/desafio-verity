using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Verity.Compartilhado.Hospedagem.Respostas
{
    public sealed class ProblemaDeNegocioResult(IReadOnlyCollection<string> codigosDeErro) : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext contexto)
        {
            var statusHttp = contexto.HttpContext.RequestServices
                .GetRequiredService<StatusHttpDosCodigosDeErro>()
                .Obter(codigosDeErro);

            return new ObjectResult(ProblemaDeNegocio.Criar(contexto.HttpContext, statusHttp, codigosDeErro))
            {
                StatusCode = statusHttp,
                ContentTypes = { ProblemaDeNegocio.TipoDeConteudo }
            }.ExecuteResultAsync(contexto);
        }
    }
}
