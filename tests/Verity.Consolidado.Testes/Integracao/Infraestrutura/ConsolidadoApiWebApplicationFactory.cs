using Verity.Testes.Compartilhado.Hospedagem;

namespace Verity.Consolidado.Testes.Integracao.Infraestrutura
{
    public sealed class ConsolidadoApiWebApplicationFactory(
        string stringDeConexaoDoBanco,
        IReadOnlyDictionary<string, string?>? configuracaoAdicional = null)
        : ApiAutenticadaWebApplicationFactory<Program>
    {
        protected override IReadOnlyDictionary<string, string?> ConfiguracaoDoServico =>
            new Dictionary<string, string?>(configuracaoAdicional ?? new Dictionary<string, string?>())
            {
                ["ConnectionStrings:Consolidado"] = stringDeConexaoDoBanco
            };
    }
}
