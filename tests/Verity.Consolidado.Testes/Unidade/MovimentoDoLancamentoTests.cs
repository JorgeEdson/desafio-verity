using Verity.Consolidado.Core.Dominio.Erros;
using Verity.Consolidado.Core.Dominio.ObjetosDeValor;
using Verity.Consolidado.Testes.Compartilhado;

namespace Verity.Consolidado.Testes.Unidade
{
    public sealed class MovimentoDoLancamentoTests
    {
        [Fact]
        public void TentarCriar_DeveGerarVariacaoPositiva_QuandoLancamentoForCredito()
        {
            var movimento = MovimentoDoLancamento.TentarCriar(new LancamentoRegistradoBuilder().Credito(8000.00m).Construir()).Instancia;

            Assert.Equal(8000.00m, movimento.TotalDeCreditos);
            Assert.Equal(0m, movimento.TotalDeDebitos);
            Assert.Equal(8000.00m, movimento.VariacaoDoSaldo);
        }

        [Fact]
        public void TentarCriar_DeveGerarVariacaoNegativa_QuandoLancamentoForDebito()
        {
            var movimento = MovimentoDoLancamento.TentarCriar(new LancamentoRegistradoBuilder().Debito(1200.00m).Construir()).Instancia;

            Assert.Equal(0m, movimento.TotalDeCreditos);
            Assert.Equal(1200.00m, movimento.TotalDeDebitos);
            Assert.Equal(-1200.00m, movimento.VariacaoDoSaldo);
        }

        [Fact]
        public void TentarCriar_DeveRejeitarEvento_QuandoVersaoNaoForSuportada()
        {
            var movimento = MovimentoDoLancamento.TentarCriar(new LancamentoRegistradoBuilder().NaVersao(2).Construir());

            Assert.Contains(CodigosDeErro.EventoComVersaoNaoSuportada, movimento.Erros);
        }

        [Fact]
        public void TentarCriar_DeveReunirTodosOsErros_QuandoEventoViolarOContrato()
        {
            var eventoInvalido = new LancamentoRegistradoBuilder()
                .ComEventoId(Guid.Empty)
                .DoComerciante(" ")
                .DoTipo("Transferencia")
                .Construir() with { Valor = 0m };

            var movimento = MovimentoDoLancamento.TentarCriar(eventoInvalido);

            Assert.Contains(CodigosDeErro.EventoSemIdentificador, movimento.Erros);
            Assert.Contains(CodigosDeErro.EventoSemComerciante, movimento.Erros);
            Assert.Contains(CodigosDeErro.EventoComTipoDeLancamentoInvalido, movimento.Erros);
            Assert.Contains(CodigosDeErro.EventoComValorInvalido, movimento.Erros);
        }
    }
}
