using System.Text;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Verity.Compartilhado.Integracao;

namespace Verity.Consolidado.Testes.Integracao.Mensageria
{
    public sealed class RabbitMqDoConsolidadoFixture : IAsyncLifetime
    {
        private const int LimiteDeEntregas = 5;

        private readonly RabbitMqContainer _rabbitMq = ConteineresDeTeste.CriarRabbitMq();

        public string StringDeConexao => _rabbitMq.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _rabbitMq.StartAsync();

            await using var conexao = await _rabbitMq.CriarConexaoAsync();
            await using var canal = await conexao.CreateChannelAsync();

            await canal.ExchangeDeclareAsync(TopologiaDeMensageria.ExchangeDeLancamentos, ExchangeType.Topic, durable: true);
            await canal.ExchangeDeclareAsync(TopologiaDeMensageria.ExchangeDeLancamentosRejeitados, ExchangeType.Fanout, durable: true);

            await canal.QueueDeclareAsync(
                TopologiaDeMensageria.FilaDeLancamentosDoConsolidado,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-queue-type"] = "quorum",
                    ["x-delivery-limit"] = LimiteDeEntregas,
                    ["x-dead-letter-exchange"] = TopologiaDeMensageria.ExchangeDeLancamentosRejeitados
                });

            await canal.QueueDeclareAsync(
                TopologiaDeMensageria.FilaDeLancamentosRejeitadosDoConsolidado,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" });

            await canal.QueueBindAsync(
                TopologiaDeMensageria.FilaDeLancamentosDoConsolidado,
                TopologiaDeMensageria.ExchangeDeLancamentos,
                TopologiaDeMensageria.ChaveDeRoteamentoDeLancamentoRegistrado);

            await canal.QueueBindAsync(
                TopologiaDeMensageria.FilaDeLancamentosRejeitadosDoConsolidado,
                TopologiaDeMensageria.ExchangeDeLancamentosRejeitados,
                string.Empty);
        }

        public Task DisposeAsync() => _rabbitMq.DisposeAsync().AsTask();

        public async Task PublicarAsync(string conteudo)
        {
            await using var conexao = await _rabbitMq.CriarConexaoAsync();
            await using var canal = await conexao.CreateChannelAsync();

            await canal.BasicPublishAsync(
                TopologiaDeMensageria.ExchangeDeLancamentos,
                TopologiaDeMensageria.ChaveDeRoteamentoDeLancamentoRegistrado,
                Encoding.UTF8.GetBytes(conteudo));
        }

        public async Task<uint> ContarMensagensRejeitadasAsync()
        {
            await using var conexao = await _rabbitMq.CriarConexaoAsync();
            await using var canal = await conexao.CreateChannelAsync();
            return await canal.MessageCountAsync(TopologiaDeMensageria.FilaDeLancamentosRejeitadosDoConsolidado);
        }
    }
}
