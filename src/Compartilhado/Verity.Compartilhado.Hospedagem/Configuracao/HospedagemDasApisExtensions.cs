using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Hospedagem.Respostas;
using Verity.Compartilhado.Saude;

namespace Verity.Compartilhado.Hospedagem.Configuracao
{
    public static class HospedagemDasApisExtensions
    {
        private const string RotaDoProcessoEmExecucao = "/health/live";
        private const string RotaDoProcessoProntoParaReceberRequisicoes = "/health/ready";

        public static IServiceCollection AdicionarDispatcherDeUseCases(this IServiceCollection servicos)
        {
            servicos.AddScoped<IDispatcherUseCase, DispatcherUseCase>();
            return servicos;
        }

        public static IServiceCollection AdicionarRespostasDeErroPadronizadas(
            this IServiceCollection servicos,
            IReadOnlyDictionary<string, int> statusHttpPorCodigoDeErro)
        {
            servicos.AddProblemDetails();
            servicos.AddSingleton(new StatusHttpDosCodigosDeErro(statusHttpPorCodigoDeErro));
            return servicos;
        }

        public static IEndpointRouteBuilder MapearHealthChecksDeVivacidadeEProntidao(this IEndpointRouteBuilder rotas)
        {
            rotas.MapHealthChecks(RotaDoProcessoEmExecucao, new HealthCheckOptions
            {
                Predicate = _ => false
            });

            rotas.MapHealthChecks(RotaDoProcessoProntoParaReceberRequisicoes, new HealthCheckOptions
            {
                Predicate = verificacao => verificacao.Tags.Contains(EtiquetasDeVerificacaoDeSaude.DependenciaObrigatoriaParaProntidao)
            });

            return rotas;
        }
    }
}
