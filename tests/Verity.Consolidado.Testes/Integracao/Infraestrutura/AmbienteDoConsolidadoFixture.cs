using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Verity.Compartilhado.Infraestrutura.Persistencia;
using Verity.Compartilhado.Integracao;
using Verity.Consolidado.Core.Dominio.Entidades;
using Verity.Consolidado.Core.Dominio.Enumeradores;
using Verity.Consolidado.Core.Dominio.ObjetosDeValor;
using Verity.Consolidado.Infraestrutura.Persistencia;

namespace Verity.Consolidado.Testes.Integracao.Infraestrutura
{
    public sealed class AmbienteDoConsolidadoFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = ConteineresDeTeste.CriarPostgreSql();

        public ConsolidadoApiWebApplicationFactory Api { get; private set; } = null!;

        public string StringDeConexaoDoBanco => _postgres.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            await using var contexto = CriarContexto();
            await contexto.Database.EnsureCreatedAsync();

            Api = new ConsolidadoApiWebApplicationFactory(StringDeConexaoDoBanco);
        }

        public async Task DisposeAsync()
        {
            await Api.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        public ConsolidadoDbContext CriarContexto(string? identificadorDoComerciante = null) =>
            new(OpcoesDoPostgreSql.Criar<ConsolidadoDbContext>(StringDeConexaoDoBanco),
                identificadorDoComerciante is null ? null : new ComercianteAtualFake(identificadorDoComerciante));

        public async Task<ResultadoDaConsolidacao> ConsolidarAsync(LancamentoRegistradoIntegrationEvent lancamentoRegistrado)
        {
            await using var contexto = CriarContexto();
            var consolidacao = new ConsolidacaoDeMovimentos(contexto, TimeProvider.System);
            return await consolidacao.ConsolidarAsync(MovimentoDoLancamento.TentarCriar(lancamentoRegistrado).Instancia, CancellationToken.None);
        }

        public async Task<IReadOnlyDictionary<DateOnly, SaldoDiario>> ObterSaldosDoComercianteAsync(string identificadorDoComerciante)
        {
            await using var contexto = CriarContexto(identificadorDoComerciante);
            var saldos = await contexto.SaldosDiarios.AsNoTracking().ToListAsync();
            return saldos.ToDictionary(saldo => saldo.Data);
        }
    }
}
