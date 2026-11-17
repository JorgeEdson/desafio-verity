using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Verity.Compartilhado.Integracao;
using Verity.Consolidado.Infraestrutura.Configuracao;
using Verity.Consolidado.Testes.Compartilhado;
using Verity.Consolidado.Testes.Integracao.Infraestrutura;

namespace Verity.Consolidado.Testes.Integracao.Mensageria
{
    [Collection(ColecaoDoAmbienteDoConsolidado.Nome)]
    public sealed class ConsumoDeLancamentosRegistradosTests(AmbienteDoConsolidadoFixture ambiente, RabbitMqDoConsolidadoFixture rabbitMq)
        : IClassFixture<RabbitMqDoConsolidadoFixture>, IAsyncLifetime
    {
        private static readonly TimeSpan TempoMaximoDeEspera = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan IntervaloEntreVerificacoes = TimeSpan.FromMilliseconds(200);

        private ServiceProvider _provedorDeServicos = null!;
        private IHostedService _consumo = null!;

        public async Task InitializeAsync()
        {
            var configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Consolidado"] = ambiente.StringDeConexaoDoBanco,
                    ["RabbitMq:Uri"] = rabbitMq.StringDeConexao
                })
                .Build();

            var servicos = new ServiceCollection();
            servicos.AddLogging();
            servicos.AddSingleton<IConfiguration>(configuracao);
            servicos.AdicionarInfraestruturaDoConsolidado();
            servicos.AdicionarConsumoDeLancamentosRegistrados(configuracao);

            _provedorDeServicos = servicos.BuildServiceProvider();
            _consumo = _provedorDeServicos.GetServices<IHostedService>().Single();
            await _consumo.StartAsync(CancellationToken.None);
        }

        public async Task DisposeAsync()
        {
            await _consumo.StopAsync(CancellationToken.None);
            await _provedorDeServicos.DisposeAsync();
        }

        [Fact]
        public async Task EventoPublicado_DeveSerConsolidadoNoSaldoDoComerciante()
        {
            var comerciante = $"c-{Guid.NewGuid():N}";
            var dataCompetencia = new DateOnly(2026, 11, 15);
            var evento = new LancamentoRegistradoBuilder().DoComerciante(comerciante).NaData(dataCompetencia).Credito(4321.00m).Construir();

            await rabbitMq.PublicarAsync(SerializacaoDeEventosDeIntegracao.Serializar(evento));

            var saldoFoiConsolidado = await AguardarAsync(async () =>
                (await ambiente.ObterSaldosDoComercianteAsync(comerciante)).TryGetValue(dataCompetencia, out var saldo)
                && saldo.SaldoFinal == 4321.00m);

            Assert.True(saldoFoiConsolidado);
        }

        [Fact]
        public async Task MensagemIlegivel_DeveSerEnviadaParaAFilaDeRejeitados()
        {
            var mensagensRejeitadasAntes = await rabbitMq.ContarMensagensRejeitadasAsync();

            await rabbitMq.PublicarAsync("{ isto não é um evento válido");

            var mensagemFoiRejeitada = await AguardarAsync(async () =>
                await rabbitMq.ContarMensagensRejeitadasAsync() > mensagensRejeitadasAntes);

            Assert.True(mensagemFoiRejeitada);
        }

        [Fact]
        public async Task EventoQueViolaOContrato_DeveSerEnviadoParaAFilaDeRejeitados()
        {
            var mensagensRejeitadasAntes = await rabbitMq.ContarMensagensRejeitadasAsync();
            var eventoComVersaoNaoSuportada = new LancamentoRegistradoBuilder().NaVersao(99).Construir();

            await rabbitMq.PublicarAsync(SerializacaoDeEventosDeIntegracao.Serializar(eventoComVersaoNaoSuportada));

            var mensagemFoiRejeitada = await AguardarAsync(async () =>
                await rabbitMq.ContarMensagensRejeitadasAsync() > mensagensRejeitadasAntes);

            Assert.True(mensagemFoiRejeitada);
        }

        private static async Task<bool> AguardarAsync(Func<Task<bool>> condicao)
        {
            var limite = DateTime.UtcNow + TempoMaximoDeEspera;

            while (DateTime.UtcNow < limite)
            {
                if (await condicao())
                    return true;

                await Task.Delay(IntervaloEntreVerificacoes);
            }

            return false;
        }
    }
}
