using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Verity.Compartilhado.Integracao;
using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Compartilhado.Infraestrutura.Mensageria;
using Verity.Lancamentos.Infraestrutura.Mensageria;
using Verity.Lancamentos.Infraestrutura.Outbox;
using Verity.Lancamentos.Testes.Compartilhado;
using Verity.Lancamentos.Testes.Integracao.Infraestrutura;

namespace Verity.Lancamentos.Testes.Integracao.Outbox
{
    [Collection(ColecaoDoAmbienteDeLancamentos.Nome)]
    public sealed class PublicacaoDeEventosPendentesTests(AmbienteDeLancamentosFixture ambiente, RabbitMqFixture rabbitMq)
        : IClassFixture<RabbitMqFixture>
    {
        private const int TamanhoDoLoteParaPublicarTodosOsPendentes = 10_000;
        private const string EnderecoDeRabbitMqInacessivel = "amqp://usuario:senha@127.0.0.1:1/";

        [Fact]
        public async Task PublicarLote_DevePublicarOEventoNaExchangeEMarcaloComoPublicado()
        {
            var lancamento = await GravarLancamentoAsync();

            await PublicarPendentesAsync(rabbitMq.StringDeConexao);

            var mensagensPublicadas = await rabbitMq.ConsumirMensagensDaFilaDeVerificacaoAsync();
            Assert.Contains(mensagensPublicadas, mensagem =>
                SerializacaoDeEventosDeIntegracao.Desserializar<LancamentoRegistradoIntegrationEvent>(mensagem)?.LancamentoId == lancamento.Id);

            var eventoNoOutbox = await ambiente.ObterEventoDoOutboxDoLancamentoAsync(lancamento.Id);
            Assert.NotNull(eventoNoOutbox!.PublicadoEmUtc);
        }

        [Fact]
        public async Task PublicarLote_DeveManterOEventoPendente_QuandoRabbitMqEstiverInacessivel()
        {
            var lancamento = await GravarLancamentoAsync();

            await PublicarPendentesAsync(EnderecoDeRabbitMqInacessivel);

            var eventoNoOutbox = await ambiente.ObterEventoDoOutboxDoLancamentoAsync(lancamento.Id);
            Assert.Null(eventoNoOutbox!.PublicadoEmUtc);
        }

        private async Task<Lancamento> GravarLancamentoAsync()
        {
            var comerciante = $"c-{Guid.NewGuid():N}";
            var lancamento = new LancamentoBuilder().DoComerciante(comerciante).Construir();

            await using var contexto = ambiente.CriarContexto(new ComercianteAtualFake(comerciante));
            contexto.Lancamentos.Add(lancamento);
            await contexto.SaveChangesAsync();

            return lancamento;
        }

        private async Task PublicarPendentesAsync(string enderecoDoRabbitMq)
        {
            await using var contexto = ambiente.CriarContexto();
            await using var publicador = new PublicadorDeEventosDeIntegracaoRabbitMq(
                Options.Create(new ConexaoRabbitMqOptions { Uri = enderecoDoRabbitMq }));

            var publicacao = new PublicacaoDeEventosPendentes(
                contexto,
                publicador,
                TimeProvider.System,
                NullLogger<PublicacaoDeEventosPendentes>.Instance);

            await publicacao.PublicarLoteAsync(TamanhoDoLoteParaPublicarTodosOsPendentes, CancellationToken.None);
        }
    }
}
