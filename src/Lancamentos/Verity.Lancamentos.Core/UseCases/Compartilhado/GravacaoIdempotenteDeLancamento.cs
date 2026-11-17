using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.Dominio.ObjetosDeValor;

namespace Verity.Lancamentos.Core.UseCases.Compartilhado
{
    public static class GravacaoIdempotenteDeLancamento
    {
        public static Resultado<RegistroDeLancamentoSaida> ResponderRequisicaoRepetida(
            Lancamento lancamentoJaRegistradoComAMesmaChave,
            HashDaRequisicao hashDaRequisicaoRepetida)
        {
            if (!lancamentoJaRegistradoComAMesmaChave.FoiRegistradoComOMesmoConteudo(hashDaRequisicaoRepetida))
                return Resultado<RegistroDeLancamentoSaida>.Falha(CodigosDeErro.ChaveDeIdempotenciaReutilizadaComConteudoDiferente);

            return Resultado<RegistroDeLancamentoSaida>.Sucesso(new RegistroDeLancamentoSaida(
                LancamentoSaida.APartirDe(lancamentoJaRegistradoComAMesmaChave),
                FoiCriadoNestaRequisicao: false));
        }

        public static async Task<Resultado<RegistroDeLancamentoSaida>> GravarAsync(
            IUnitOfWork unitOfWork,
            Lancamento novoLancamento,
            HashDaRequisicao hashDaRequisicao,
            CancellationToken cancellationToken)
        {
            unitOfWork.Lancamentos.Adicionar(novoLancamento);

            var confirmacao = await unitOfWork.ConfirmarAlteracoesAsync(cancellationToken);

            if (confirmacao.EhSucesso)
            {
                return Resultado<RegistroDeLancamentoSaida>.Sucesso(new RegistroDeLancamentoSaida(
                    LancamentoSaida.APartirDe(novoLancamento),
                    FoiCriadoNestaRequisicao: true));
            }

            if (!confirmacao.PossuiErro(CodigosDeErro.ChaveDeIdempotenciaJaUtilizada))
                return Resultado<RegistroDeLancamentoSaida>.Falha(confirmacao.Erros);

            var lancamentoGravadoPorRequisicaoConcorrente = await unitOfWork.Lancamentos
                .ObterPorChaveDeIdempotenciaAsync(novoLancamento.ChaveDeIdempotencia, cancellationToken);

            return lancamentoGravadoPorRequisicaoConcorrente is null
                ? Resultado<RegistroDeLancamentoSaida>.Falha(confirmacao.Erros)
                : ResponderRequisicaoRepetida(lancamentoGravadoPorRequisicaoConcorrente, hashDaRequisicao);
        }
    }
}
