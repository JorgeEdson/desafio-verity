using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Infraestrutura.Mensageria;
using Verity.Compartilhado.Infraestrutura.Persistencia;
using Verity.Consolidado.Core.Contratos;
using Verity.Consolidado.Core.Dominio.Enumeradores;
using Verity.Consolidado.Core.UseCases.ConsolidarLancamentoRegistrado;
using Verity.Consolidado.Infraestrutura.Mensageria;
using Verity.Consolidado.Infraestrutura.Persistencia;
using Verity.Consolidado.Infraestrutura.Persistencia.Repositorios;

namespace Verity.Consolidado.Infraestrutura.Configuracao
{
    public static class InfraestruturaDoConsolidadoExtensions
    {
        private const string NomeDaStringDeConexao = "Consolidado";

        public static IServiceCollection AdicionarInfraestruturaDoConsolidado(this IServiceCollection servicos)
        {
            servicos.AdicionarBancoPostgreSql<ConsolidadoDbContext>(NomeDaStringDeConexao);

            servicos.AddScoped<ISaldoDiarioRepository, SaldoDiarioRepository>();
            servicos.AddScoped<IConsolidacaoDeMovimentos, ConsolidacaoDeMovimentos>();
            servicos.TryAddSingleton(TimeProvider.System);

            return servicos;
        }

        public static IServiceCollection AdicionarConsumoDeLancamentosRegistrados(
            this IServiceCollection servicos,
            IConfiguration configuracao)
        {
            servicos.AdicionarConexaoRabbitMq(configuracao);
            servicos.AddOptions<ConsumoDeLancamentosRegistradosOptions>()
                .Bind(configuracao.GetSection(ConsumoDeLancamentosRegistradosOptions.Secao));

            servicos.AddMetrics();
            servicos.AddSingleton<MetricasDaConsolidacao>();
            servicos.AddScoped<IUseCase<ConsolidarLancamentoRegistradoEntrada, ResultadoDaConsolidacao>, ConsolidarLancamentoRegistradoUseCase>();
            servicos.AddHostedService<ConsumoDeLancamentosRegistradosBackgroundService>();

            return servicos;
        }
    }
}
