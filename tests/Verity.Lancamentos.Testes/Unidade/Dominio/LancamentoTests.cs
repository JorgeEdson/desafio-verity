using Verity.Lancamentos.Core.Dominio.Enumeradores;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.Dominio.Eventos;
using Verity.Lancamentos.Core.Dominio.ObjetosDeValor;
using Verity.Lancamentos.Testes.Compartilhado;

namespace Verity.Lancamentos.Testes.Unidade.Dominio
{
    public sealed class LancamentoTests
    {
        private static readonly DateTime InstanteDoEstornoUtc = new(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Registrar_DeveGerarEventoLancamentoRegistradoComOsDadosDoLancamento()
        {
            var lancamento = new LancamentoBuilder().DoTipo(TipoDeLancamento.Debito).ComValor(1200.00m).Construir();

            var evento = Assert.IsType<LancamentoRegistrado>(Assert.Single(lancamento.EventosDeDominio));
            Assert.Equal(lancamento.Id, evento.IdDoAgregado);
            Assert.Equal(TipoDeLancamento.Debito, evento.Tipo);
            Assert.Equal(1200.00m, evento.Valor);
            Assert.Equal(lancamento.DataCompetencia, evento.DataCompetencia);
        }

        [Theory]
        [InlineData(TipoDeLancamento.Credito, TipoDeLancamento.Debito)]
        [InlineData(TipoDeLancamento.Debito, TipoDeLancamento.Credito)]
        public void Estornar_DeveCriarLancamentoDeTipoInversoComMesmoValorEMesmaDataDeCompetencia(
            TipoDeLancamento tipoOriginal,
            TipoDeLancamento tipoEsperadoDoEstorno)
        {
            var original = new LancamentoBuilder().DoTipo(tipoOriginal).ComValor(80.50m).Construir();

            var estorno = original.Estornar(NovaChave(), HashDaRequisicao.Calcular("estorno"), InstanteDoEstornoUtc).Instancia;

            Assert.Equal(tipoEsperadoDoEstorno, estorno.Tipo);
            Assert.Equal(original.Valor, estorno.Valor);
            Assert.Equal(original.DataCompetencia, estorno.DataCompetencia);
            Assert.Equal(original.Id, estorno.EstornoDeId);
            Assert.True(estorno.EhEstorno);
            Assert.IsType<LancamentoRegistrado>(Assert.Single(estorno.EventosDeDominio));
        }

        [Fact]
        public void Estornar_DeveFalhar_QuandoLancamentoJaForUmEstorno()
        {
            var original = new LancamentoBuilder().Construir();
            var estorno = original.Estornar(NovaChave(), HashDaRequisicao.Calcular("estorno"), InstanteDoEstornoUtc).Instancia;

            var estornoDoEstorno = estorno.Estornar(NovaChave(), HashDaRequisicao.Calcular("estorno"), InstanteDoEstornoUtc);

            Assert.True(estornoDoEstorno.EhFalha);
            Assert.Contains(CodigosDeErro.EstornoNaoPodeSerEstornado, estornoDoEstorno.Erros);
        }

        [Fact]
        public void Estornar_DeveLimitarADescricaoAoTamanhoMaximo_QuandoOPrefixoUltrapassarOLimite()
        {
            var descricaoNoTamanhoMaximo = new string('x', DescricaoDoLancamento.TamanhoMaximo);
            var original = new LancamentoBuilder().ComDescricao(descricaoNoTamanhoMaximo).Construir();

            var estorno = original.Estornar(NovaChave(), HashDaRequisicao.Calcular("estorno"), InstanteDoEstornoUtc).Instancia;

            Assert.Equal(DescricaoDoLancamento.TamanhoMaximo, estorno.Descricao.Length);
            Assert.StartsWith("Estorno: ", estorno.Descricao);
        }

        private static ChaveDeIdempotencia NovaChave() =>
            ChaveDeIdempotencia.TentarCriar(Guid.NewGuid().ToString()).Instancia;
    }
}
