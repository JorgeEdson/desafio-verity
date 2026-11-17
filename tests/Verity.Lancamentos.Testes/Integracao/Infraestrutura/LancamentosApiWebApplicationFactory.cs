using Verity.Testes.Compartilhado.Hospedagem;

namespace Verity.Lancamentos.Testes.Integracao.Infraestrutura
{
    public sealed class LancamentosApiWebApplicationFactory(string stringDeConexaoDoBanco) : ApiAutenticadaWebApplicationFactory<Program>
    {
        private const string EnderecoDeRabbitMqInacessivel = "amqp://usuario:senha@127.0.0.1:1/";

        protected override IReadOnlyDictionary<string, string?> ConfiguracaoDoServico => new Dictionary<string, string?>
        {
            ["ConnectionStrings:Lancamentos"] = stringDeConexaoDoBanco,
            ["RabbitMq:Uri"] = EnderecoDeRabbitMqInacessivel,
            ["Outbox:Habilitada"] = bool.FalseString
        };
    }
}
