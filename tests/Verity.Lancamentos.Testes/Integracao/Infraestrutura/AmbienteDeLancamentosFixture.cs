using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Infraestrutura.Persistencia;
using Verity.Compartilhado.Integracao;
using Verity.Lancamentos.Infraestrutura.Outbox;
using Verity.Lancamentos.Infraestrutura.Persistencia;

namespace Verity.Lancamentos.Testes.Integracao.Infraestrutura
{
    public sealed class AmbienteDeLancamentosFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = ConteineresDeTeste.CriarPostgreSql();

        public LancamentosApiWebApplicationFactory Api { get; private set; } = null!;

        public string StringDeConexaoDoBanco => _postgres.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            await using var contexto = CriarContexto();
            await contexto.Database.EnsureCreatedAsync();

            Api = new LancamentosApiWebApplicationFactory(StringDeConexaoDoBanco);
        }

        public async Task DisposeAsync()
        {
            await Api.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        public LancamentosDbContext CriarContexto(IComercianteAtual? comercianteAtual = null) =>
            new(OpcoesDoPostgreSql.Criar<LancamentosDbContext>(StringDeConexaoDoBanco, new CapturaDeEventosParaOutboxInterceptor()),
                comercianteAtual);

        public async Task<EventoNoOutbox?> ObterEventoDoOutboxDoLancamentoAsync(Guid idDoLancamento)
        {
            await using var contexto = CriarContexto();
            var eventosNoOutbox = await contexto.EventosNoOutbox.AsNoTracking().ToListAsync();

            return eventosNoOutbox.SingleOrDefault(evento =>
                SerializacaoDeEventosDeIntegracao
                    .Desserializar<LancamentoRegistradoIntegrationEvent>(evento.Conteudo)?
                    .LancamentoId == idDoLancamento);
        }
    }
}
