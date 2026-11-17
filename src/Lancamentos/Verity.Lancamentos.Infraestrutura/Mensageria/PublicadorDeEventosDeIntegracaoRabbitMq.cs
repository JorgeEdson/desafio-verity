using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Verity.Compartilhado.Infraestrutura.Mensageria;
using Verity.Compartilhado.Integracao;
using Verity.Compartilhado.Observabilidade;
using Verity.Lancamentos.Infraestrutura.Outbox;

namespace Verity.Lancamentos.Infraestrutura.Mensageria
{
    public sealed class PublicadorDeEventosDeIntegracaoRabbitMq(IOptions<ConexaoRabbitMqOptions> opcoes)
        : IPublicadorDeEventosDeIntegracao, IAsyncDisposable
    {
        private const string TipoDeConteudoJson = "application/json";

        private static readonly CreateChannelOptions OpcoesDoCanalComConfirmacaoDePublicacao = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        private readonly SemaphoreSlim _acessoExclusivoAoCanal = new(1, 1);
        private IConnection? _conexao;
        private IChannel? _canal;

        public async Task PublicarAsync(EventoNoOutbox evento, CancellationToken cancellationToken)
        {
            using var atividade = RastreamentoDeMensageria.IniciarPublicacao(
                TopologiaDeMensageria.ExchangeDeLancamentos,
                evento.EventoId.ToString(),
                evento.ContextoDeRastreamento);

            await _acessoExclusivoAoCanal.WaitAsync(cancellationToken);

            try
            {
                var canal = await ObterCanalAbertoAsync(cancellationToken);

                await canal.BasicPublishAsync(
                    exchange: TopologiaDeMensageria.ExchangeDeLancamentos,
                    routingKey: evento.ChaveDeRoteamento,
                    mandatory: true,
                    basicProperties: MontarPropriedadesDaMensagem(evento, atividade?.Id ?? evento.ContextoDeRastreamento),
                    body: Encoding.UTF8.GetBytes(evento.Conteudo),
                    cancellationToken: cancellationToken);
            }
            catch (Exception excecao)
            {
                atividade?.SetStatus(ActivityStatusCode.Error, excecao.Message);
                await DescartarConexaoIgnorandoFalhasAsync();
                throw;
            }
            finally
            {
                _acessoExclusivoAoCanal.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DescartarConexaoIgnorandoFalhasAsync();
            _acessoExclusivoAoCanal.Dispose();
        }

        private async Task<IChannel> ObterCanalAbertoAsync(CancellationToken cancellationToken)
        {
            if (_canal is { IsOpen: true })
                return _canal;

            await DescartarConexaoIgnorandoFalhasAsync();

            _conexao = await opcoes.Value.CriarFabricaDeConexoes().CreateConnectionAsync(cancellationToken);
            _canal = await _conexao.CreateChannelAsync(OpcoesDoCanalComConfirmacaoDePublicacao, cancellationToken);
            return _canal;
        }

        private static BasicProperties MontarPropriedadesDaMensagem(EventoNoOutbox evento, string? contextoDeRastreamento) => new()
        {
            Persistent = true,
            MessageId = evento.EventoId.ToString(),
            Type = evento.TipoDoEvento,
            ContentType = TipoDeConteudoJson,
            Timestamp = new AmqpTimestamp(new DateTimeOffset(evento.OcorridoEmUtc).ToUnixTimeSeconds()),
            Headers = MontarCabecalhosDaMensagem(contextoDeRastreamento)
        };

        private static Dictionary<string, object?> MontarCabecalhosDaMensagem(string? contextoDeRastreamento)
        {
            var cabecalhos = new Dictionary<string, object?>();

            if (contextoDeRastreamento is not null)
                cabecalhos[RastreamentoDeMensageria.CabecalhoDeContextoDeRastreamento] = contextoDeRastreamento;

            return cabecalhos;
        }

        private async Task DescartarConexaoIgnorandoFalhasAsync()
        {
            await DescartarIgnorandoFalhasAsync(_canal);
            await DescartarIgnorandoFalhasAsync(_conexao);
            _canal = null;
            _conexao = null;
        }

        private static async Task DescartarIgnorandoFalhasAsync(IAsyncDisposable? recurso)
        {
            if (recurso is null)
                return;

            try
            {
                await recurso.DisposeAsync();
            }
            catch (Exception)
            {
            }
        }
    }
}
