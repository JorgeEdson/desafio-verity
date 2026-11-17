using RabbitMQ.Client;

namespace Verity.Compartilhado.Infraestrutura.Mensageria
{
    public sealed class ConexaoRabbitMqOptions
    {
        public const string Secao = "RabbitMq";

        public string Uri { get; set; } = string.Empty;

        public string NomeDaConexao { get; set; } = "verity";

        public TimeSpan TempoLimiteDeConexao { get; set; } = TimeSpan.FromSeconds(5);

        public ConnectionFactory CriarFabricaDeConexoes() => new()
        {
            Uri = new System.Uri(Uri),
            ClientProvidedName = NomeDaConexao,
            RequestedConnectionTimeout = TempoLimiteDeConexao,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
    }
}
