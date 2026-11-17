using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Compartilhado.Hospedagem.Respostas;
using Verity.Lancamentos.Api.Modelos;
using Verity.Lancamentos.Core.UseCases.Compartilhado;
using Verity.Lancamentos.Core.UseCases.EstornarLancamento;
using Verity.Lancamentos.Core.UseCases.ListarLancamentosDoPeriodo;
using Verity.Lancamentos.Core.UseCases.ObterLancamentoPorId;
using Verity.Lancamentos.Core.UseCases.RegistrarLancamento;

namespace Verity.Lancamentos.Api.Controllers
{
    [Route("lancamentos")]
    public sealed class LancamentosController(IDispatcherUseCase dispatcherUseCase) : ControllerBaseDaApi
    {
        private const string CabecalhoDaChaveDeIdempotencia = "Idempotency-Key";
        private const int PaginaPadrao = 1;
        private const int TamanhoPadraoDaPagina = 50;

        [HttpPost]
        public async Task<IActionResult> Registrar(
            [FromBody] RegistrarLancamentoRequest requisicao,
            [FromHeader(Name = CabecalhoDaChaveDeIdempotencia)] string? chaveDeIdempotencia,
            CancellationToken cancellationToken)
        {
            var resultado = await dispatcherUseCase.EnviarAsync(
                new RegistrarLancamentoEntrada(
                    requisicao.Tipo,
                    requisicao.Valor.GetValueOrDefault(),
                    requisicao.DataCompetencia.GetValueOrDefault(),
                    requisicao.Descricao,
                    chaveDeIdempotencia ?? string.Empty),
                cancellationToken);

            return ResponderRegistro(resultado);
        }

        [HttpPost("{id:guid}/estornos")]
        public async Task<IActionResult> Estornar(
            Guid id,
            [FromHeader(Name = CabecalhoDaChaveDeIdempotencia)] string? chaveDeIdempotencia,
            CancellationToken cancellationToken)
        {
            var resultado = await dispatcherUseCase.EnviarAsync(
                new EstornarLancamentoEntrada(id, chaveDeIdempotencia ?? string.Empty),
                cancellationToken);

            return ResponderRegistro(resultado);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
        {
            var resultado = await dispatcherUseCase.EnviarAsync(new ObterLancamentoPorIdEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> ListarDoPeriodo(
            [FromQuery, BindRequired] DateOnly inicio,
            [FromQuery, BindRequired] DateOnly fim,
            CancellationToken cancellationToken,
            [FromQuery] int pagina = PaginaPadrao,
            [FromQuery] int tamanhoDaPagina = TamanhoPadraoDaPagina)
        {
            var resultado = await dispatcherUseCase.EnviarAsync(
                new ListarLancamentosDoPeriodoEntrada(inicio, fim, pagina, tamanhoDaPagina),
                cancellationToken);

            return Responder(resultado);
        }

        private IActionResult ResponderRegistro(Resultado<RegistroDeLancamentoSaida> resultado)
        {
            if (resultado.EhFalha)
                return ResponderFalha(resultado);

            var registro = resultado.Instancia;

            return registro.FoiCriadoNestaRequisicao
                ? Created($"/lancamentos/{registro.Lancamento.Id}", registro.Lancamento)
                : Ok(registro.Lancamento);
        }
    }
}
