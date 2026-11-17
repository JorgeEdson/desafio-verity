using System.Globalization;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Lancamentos.Core.Dominio.Enumeradores;
using Verity.Lancamentos.Core.Dominio.ObjetosDeValor;
using Verity.Lancamentos.Core.UseCases.Compartilhado;

namespace Verity.Lancamentos.Core.UseCases.RegistrarLancamento
{
    public sealed class RegistrarLancamentoUseCase(
        IUnitOfWork unitOfWork,
        IComercianteAtual comercianteAtual,
        IRelogioDoComerciante relogioDoComerciante)
        : IUseCase<RegistrarLancamentoEntrada, RegistroDeLancamentoSaida>
    {
        private const string FormatoCanonicoDoValor = "0.00";
        private const string FormatoCanonicoDaData = "yyyy-MM-dd";

        public async Task<Resultado<RegistroDeLancamentoSaida>> ExecutarAsync(
            RegistrarLancamentoEntrada entrada,
            CancellationToken cancellationToken = default)
        {
            var comerciante = IdentificadorDoComerciante.TentarCriar(comercianteAtual.Identificador);
            var tipo = TiposDeLancamento.TentarConverter(entrada.Tipo);
            var valor = ValorDoLancamento.TentarCriar(entrada.Valor);
            var dataCompetencia = DataDeCompetencia.TentarCriar(entrada.DataCompetencia, relogioDoComerciante.ObterDataDeHoje());
            var descricao = DescricaoDoLancamento.TentarCriar(entrada.Descricao);
            var chaveDeIdempotencia = ChaveDeIdempotencia.TentarCriar(entrada.ChaveDeIdempotencia);

            var validacaoDaEntrada = Resultado.Combinar(comerciante, tipo, valor, dataCompetencia, descricao, chaveDeIdempotencia);

            if (validacaoDaEntrada.EhFalha)
                return Resultado<RegistroDeLancamentoSaida>.Falha(validacaoDaEntrada.Erros);

            var hashDaRequisicao = CalcularHashDaRequisicao(tipo.Instancia, valor.Instancia, dataCompetencia.Instancia, descricao.Instancia);

            var lancamentoJaRegistradoComAMesmaChave = await unitOfWork.Lancamentos
                .ObterPorChaveDeIdempotenciaAsync(chaveDeIdempotencia.Instancia.Valor, cancellationToken);

            if (lancamentoJaRegistradoComAMesmaChave is not null)
                return GravacaoIdempotenteDeLancamento.ResponderRequisicaoRepetida(lancamentoJaRegistradoComAMesmaChave, hashDaRequisicao);

            var lancamento = Lancamento.Registrar(
                comerciante.Instancia,
                tipo.Instancia,
                valor.Instancia,
                dataCompetencia.Instancia,
                descricao.Instancia,
                chaveDeIdempotencia.Instancia,
                hashDaRequisicao,
                relogioDoComerciante.ObterInstanteAtualUtc());

            return await GravacaoIdempotenteDeLancamento.GravarAsync(unitOfWork, lancamento, hashDaRequisicao, cancellationToken);
        }

        private static HashDaRequisicao CalcularHashDaRequisicao(
            TipoDeLancamento tipo,
            ValorDoLancamento valor,
            DataDeCompetencia dataCompetencia,
            DescricaoDoLancamento descricao) =>
            HashDaRequisicao.Calcular(
                tipo.ToString(),
                valor.Valor.ToString(FormatoCanonicoDoValor, CultureInfo.InvariantCulture),
                dataCompetencia.Valor.ToString(FormatoCanonicoDaData, CultureInfo.InvariantCulture),
                descricao.Valor);
    }
}
