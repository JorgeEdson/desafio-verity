using Microsoft.AspNetCore.Http;
using Verity.Compartilhado.Aplicacao;

namespace Verity.Compartilhado.Seguranca
{
    public sealed class ComercianteAtualDaRequisicao(IHttpContextAccessor acessoAoContextoHttp) : IComercianteAtual
    {
        public string Identificador =>
            acessoAoContextoHttp.HttpContext?.User.FindFirst(ClaimsDoToken.IdentificadorDoComerciante)?.Value
            ?? string.Empty;
    }
}
