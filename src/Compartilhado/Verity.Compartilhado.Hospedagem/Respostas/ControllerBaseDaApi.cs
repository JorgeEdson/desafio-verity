using Microsoft.AspNetCore.Mvc;
using Verity.Compartilhado.Base;

namespace Verity.Compartilhado.Hospedagem.Respostas
{
    [ApiController]
    public abstract class ControllerBaseDaApi : ControllerBase
    {
        protected IActionResult Responder<T>(Resultado<T> resultado) =>
            resultado.EhSucesso ? Ok(resultado.Instancia) : ResponderFalha(resultado);

        protected static IActionResult ResponderFalha(Resultado resultado) =>
            new ProblemaDeNegocioResult(resultado.Erros);
    }
}
