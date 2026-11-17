using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Compartilhado.Hospedagem.Configuracao;
using Verity.Lancamentos.Core.UseCases.Compartilhado;
using Verity.Lancamentos.Core.UseCases.EstornarLancamento;
using Verity.Lancamentos.Core.UseCases.ListarLancamentosDoPeriodo;
using Verity.Lancamentos.Core.UseCases.ObterLancamentoPorId;
using Verity.Lancamentos.Core.UseCases.RegistrarLancamento;

namespace Verity.Lancamentos.Api.Configuracao
{
    public static class CasosDeUsoExtensions
    {
        public static IServiceCollection AdicionarCasosDeUsoDeLancamentos(this IServiceCollection servicos)
        {
            servicos.AdicionarDispatcherDeUseCases();

            servicos.AddScoped<IUseCase<RegistrarLancamentoEntrada, RegistroDeLancamentoSaida>, RegistrarLancamentoUseCase>();
            servicos.AddScoped<IUseCase<EstornarLancamentoEntrada, RegistroDeLancamentoSaida>, EstornarLancamentoUseCase>();
            servicos.AddScoped<IUseCase<ObterLancamentoPorIdEntrada, LancamentoSaida>, ObterLancamentoPorIdUseCase>();
            servicos.AddScoped<IUseCase<ListarLancamentosDoPeriodoEntrada, ResultadoPaginado<LancamentoSaida>>, ListarLancamentosDoPeriodoUseCase>();

            return servicos;
        }
    }
}
