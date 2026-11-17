using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OutputCaching;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Hospedagem.Respostas;
using Verity.Consolidado.Api.Seguranca;
using Verity.Consolidado.Core.UseCases.ListarSaldosDoPeriodo;
using Verity.Consolidado.Core.UseCases.ObterSaldoDoDia;

namespace Verity.Consolidado.Api.Controllers
{
    [Route("consolidado/diario")]
    public sealed class SaldosDiariosController(IDispatcherUseCase dispatcherUseCase) : ControllerBaseDaApi
    {
        [HttpGet("{data}")]
        [OutputCache(PolicyName = PoliticasDeCache.SaldoConsolidadoPorComerciante)]
        public async Task<IActionResult> ObterDoDia(DateOnly data, CancellationToken cancellationToken)
        {
            var resultado = await dispatcherUseCase.EnviarAsync(new ObterSaldoDoDiaEntrada(data), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet]
        [OutputCache(PolicyName = PoliticasDeCache.SaldoConsolidadoPorComerciante)]
        public async Task<IActionResult> ListarDoPeriodo(
            [FromQuery, BindRequired] DateOnly inicio,
            [FromQuery, BindRequired] DateOnly fim,
            CancellationToken cancellationToken)
        {
            var resultado = await dispatcherUseCase.EnviarAsync(new ListarSaldosDoPeriodoEntrada(inicio, fim), cancellationToken);
            return Responder(resultado);
        }
    }
}
