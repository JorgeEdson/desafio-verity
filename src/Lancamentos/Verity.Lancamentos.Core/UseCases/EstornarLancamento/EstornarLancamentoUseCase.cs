using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.Dominio.ObjetosDeValor;
using Verity.Lancamentos.Core.UseCases.Compartilhado;

namespace Verity.Lancamentos.Core.UseCases.EstornarLancamento
{
    public sealed class EstornarLancamentoUseCase(
        IUnitOfWork unitOfWork,
        IComercianteAtual comercianteAtual,
        IRelogioDoComerciante relogioDoComerciante)
        : IUseCase<EstornarLancamentoEntrada, RegistroDeLancamentoSaida>
    {
        private const string IdentificacaoDaOperacaoDeEstorno = "estorno";

        public async Task<Resultado<RegistroDeLancamentoSaida>> ExecutarAsync(
            EstornarLancamentoEntrada entrada,
            CancellationToken cancellationToken = default)
        {
            var comerciante = IdentificadorDoComerciante.TentarCriar(comercianteAtual.Identificador);
            var chaveDeIdempotencia = ChaveDeIdempotencia.TentarCriar(entrada.ChaveDeIdempotencia);

            var validacaoDaEntrada = Resultado.Combinar(comerciante, chaveDeIdempotencia);

            if (validacaoDaEntrada.EhFalha)
                return Resultado<RegistroDeLancamentoSaida>.Falha(validacaoDaEntrada.Erros);

            var hashDaRequisicao = HashDaRequisicao.Calcular(IdentificacaoDaOperacaoDeEstorno, entrada.IdDoLancamento.ToString());

            var lancamentoJaRegistradoComAMesmaChave = await unitOfWork.Lancamentos
                .ObterPorChaveDeIdempotenciaAsync(chaveDeIdempotencia.Instancia.Valor, cancellationToken);

            if (lancamentoJaRegistradoComAMesmaChave is not null)
                return GravacaoIdempotenteDeLancamento.ResponderRequisicaoRepetida(lancamentoJaRegistradoComAMesmaChave, hashDaRequisicao);

            var lancamentoOriginal = await unitOfWork.Lancamentos.ObterPorIdAsync(entrada.IdDoLancamento, cancellationToken);

            if (lancamentoOriginal is null)
                return Resultado<RegistroDeLancamentoSaida>.Falha(CodigosDeErro.LancamentoNaoEncontrado);

            if (await unitOfWork.Lancamentos.ExisteEstornoDoLancamentoAsync(lancamentoOriginal.Id, cancellationToken))
                return Resultado<RegistroDeLancamentoSaida>.Falha(CodigosDeErro.LancamentoJaEstornado);

            var estorno = lancamentoOriginal.Estornar(
                chaveDeIdempotencia.Instancia,
                hashDaRequisicao,
                relogioDoComerciante.ObterInstanteAtualUtc());

            if (estorno.EhFalha)
                return Resultado<RegistroDeLancamentoSaida>.Falha(estorno.Erros);

            return await GravacaoIdempotenteDeLancamento.GravarAsync(unitOfWork, estorno.Instancia, hashDaRequisicao, cancellationToken);
        }
    }
}
