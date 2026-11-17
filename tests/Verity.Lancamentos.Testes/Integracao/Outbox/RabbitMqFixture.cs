using System.Text;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Verity.Compartilhado.Integracao;

namespace Verity.Lancamentos.Testes.Integracao.Outbox
{
    public sealed class RabbitMqFixture : IAsyncLifetime
    {
        public const string FilaDeVerificacao = "testes.lancamentos.registrados";

        private readonly RabbitMqContainer _rabbitMq = ConteineresDeTeste.CriarRabbitMq();

        public string StringDeConexao => _rabbitMq.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _rabbitMq.StartAsync();

            await using var conexao = await _rabbitMq.CriarConexaoAsync();
            await using var canal = await conexao.CreateChannelAsync();

            await canal.ExchangeDeclareAsync(TopologiaDeMensageria.ExchangeDeLancamentos, ExchangeType.Topic, durable: true);
            await canal.QueueDeclareAsync(FilaDeVerificacao, durable: true, exclusive: false, autoDelete: false);
            await canal.QueueBindAsync(FilaDeVerificacao, TopologiaDeMensageria.ExchangeDeLancamentos, TopologiaDeMensageria.ChaveDeRoteamentoDeLancamentoRegistrado);
        }

        public Task DisposeAsync() => _rabbitMq.DisposeAsync().AsTask();

        public async Task<IReadOnlyList<string>> ConsumirMensagensDaFilaDeVerificacaoAsync()
        {
            await using var conexao = await _rabbitMq.CriarConexaoAsync();
            await using var canal = await conexao.CreateChannelAsync();

            var mensagens = new List<string>();

            while (await canal.BasicGetAsync(FilaDeVerificacao, autoAck: true) is { } mensagem)
                mensagens.Add(Encoding.UTF8.GetString(mensagem.Body.Span));

            return mensagens;
        }
    }
}
