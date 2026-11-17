using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Hospedagem.Configuracao;
using Verity.Consolidado.Core.Dominio.ModelosDeLeitura;
using Verity.Consolidado.Core.UseCases.ListarSaldosDoPeriodo;
using Verity.Consolidado.Core.UseCases.ObterSaldoDoDia;

namespace Verity.Consolidado.Api.Configuracao
{
    public static class CasosDeUsoExtensions
    {
        public static IServiceCollection AdicionarCasosDeUsoDeConsulta(this IServiceCollection servicos)
        {
            servicos.AdicionarDispatcherDeUseCases();

            servicos.AddScoped<IUseCase<ObterSaldoDoDiaEntrada, SaldoConsolidadoDoDia>, ObterSaldoDoDiaUseCase>();
            servicos.AddScoped<IUseCase<ListarSaldosDoPeriodoEntrada, IReadOnlyList<SaldoConsolidadoDoDia>>, ListarSaldosDoPeriodoUseCase>();

            return servicos;
        }
    }
}
