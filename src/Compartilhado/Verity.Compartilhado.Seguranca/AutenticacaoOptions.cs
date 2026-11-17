namespace Verity.Compartilhado.Seguranca
{
    public sealed class AutenticacaoOptions
    {
        public const string Secao = "Autenticacao";

        public string Emissor { get; set; } = string.Empty;

        public string EnderecoDeMetadados { get; set; } = string.Empty;

        public string Audiencia { get; set; } = string.Empty;

        public bool ExigirHttpsNosMetadados { get; set; } = true;
    }
}
