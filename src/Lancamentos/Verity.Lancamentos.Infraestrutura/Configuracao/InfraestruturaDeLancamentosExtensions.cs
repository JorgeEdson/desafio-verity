using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Verity.Compartilhado.Infraestrutura.Mensageria;
using Verity.Compartilhado.Infraestrutura.Persistencia;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Infraestrutura.Mensageria;
using Verity.Lancamentos.Infraestrutura.Outbox;
using Verity.Lancamentos.Infraestrutura.Persistencia;
using Verity.Lancamentos.Infraestrutura.Persistencia.Repositorios;
using Verity.Lancamentos.Infraestrutura.Tempo;

namespace Verity.Lancamentos.Infraestrutura.Configuracao
{
    public static class InfraestruturaDeLancamentosExtensions
    {
        private const string NomeDaStringDeConexao = "Lancamentos";

        public static IServiceCollection AdicionarInfraestruturaDeLancamentos(
            this IServiceCollection servicos,
            IConfiguration configuracao)
        {
            servicos.AdicionarBancoPostgreSql<LancamentosDbContext>(NomeDaStringDeConexao, new CapturaDeEventosParaOutboxInterceptor());
            servicos.AdicionarConexaoRabbitMq(configuracao);

            servicos.AddScoped<ILancamentoRepository, LancamentoRepository>();
            servicos.AddScoped<IUnitOfWork, UnitOfWork>();

            servicos.TryAddSingleton(TimeProvider.System);
            servicos.AddSingleton<IRelogioDoComerciante, RelogioDoComerciante>();

            servicos.AddOptions<PublicacaoDoOutboxOptions>().Bind(configuracao.GetSection(PublicacaoDoOutboxOptions.Secao));
            servicos.AddSingleton<IPublicadorDeEventosDeIntegracao, PublicadorDeEventosDeIntegracaoRabbitMq>();
            servicos.AddScoped<PublicacaoDeEventosPendentes>();
            servicos.AddMetrics();
            servicos.AddSingleton<MetricasDoOutbox>();
            servicos.AddHostedService<PublicacaoDoOutboxBackgroundService>();

            return servicos;
        }
    }
}
