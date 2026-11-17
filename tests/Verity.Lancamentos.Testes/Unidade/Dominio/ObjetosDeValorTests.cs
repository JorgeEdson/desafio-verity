using Verity.Lancamentos.Core.Dominio.Enumeradores;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.Dominio.ObjetosDeValor;

namespace Verity.Lancamentos.Testes.Unidade.Dominio
{
    public sealed class ObjetosDeValorTests
    {
        private static readonly DateOnly DataDeHojeDoComerciante = new(2026, 11, 16);

        [Theory]
        [InlineData("0", CodigosDeErro.ValorDoLancamentoDeveSerPositivo)]
        [InlineData("-10.00", CodigosDeErro.ValorDoLancamentoDeveSerPositivo)]
        [InlineData("10.001", CodigosDeErro.ValorDoLancamentoComMaisDeDuasCasasDecimais)]
        [InlineData("1000000000000.00", CodigosDeErro.ValorDoLancamentoAcimaDoLimite)]
        public void ValorDoLancamento_DeveSerRejeitado_QuandoForInvalido(string valorInformado, string codigoDeErroEsperado)
        {
            var valor = ValorDoLancamento.TentarCriar(decimal.Parse(valorInformado, System.Globalization.CultureInfo.InvariantCulture));

            Assert.Contains(codigoDeErroEsperado, valor.Erros);
        }

        [Theory]
        [InlineData("0.01")]
        [InlineData("10.10")]
        [InlineData("999999999999.99")]
        public void ValorDoLancamento_DeveSerAceito_QuandoForPositivoComAteDuasCasasDecimais(string valorInformado)
        {
            var valor = ValorDoLancamento.TentarCriar(decimal.Parse(valorInformado, System.Globalization.CultureInfo.InvariantCulture));

            Assert.True(valor.EhSucesso);
        }

        [Fact]
        public void DataDeCompetencia_DeveSerRejeitada_QuandoEstiverNoFuturo()
        {
            var data = DataDeCompetencia.TentarCriar(DataDeHojeDoComerciante.AddDays(1), DataDeHojeDoComerciante);

            Assert.Contains(CodigosDeErro.DataDeCompetenciaNoFuturo, data.Erros);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-365)]
        public void DataDeCompetencia_DeveSerAceita_QuandoForHojeOuRetroativa(int diasEmRelacaoAHoje)
        {
            var data = DataDeCompetencia.TentarCriar(DataDeHojeDoComerciante.AddDays(diasEmRelacaoAHoje), DataDeHojeDoComerciante);

            Assert.True(data.EhSucesso);
        }

        [Theory]
        [InlineData("Credito", TipoDeLancamento.Credito)]
        [InlineData("debito", TipoDeLancamento.Debito)]
        public void TipoDeLancamento_DeveSerConvertido_QuandoForUmNomeValido(string tipoInformado, TipoDeLancamento tipoEsperado)
        {
            var tipo = TiposDeLancamento.TentarConverter(tipoInformado);

            Assert.Equal(tipoEsperado, tipo.Instancia);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("Transferencia")]
        [InlineData("")]
        [InlineData(null)]
        public void TipoDeLancamento_DeveSerRejeitado_QuandoNaoForUmNomeValido(string? tipoInformado)
        {
            var tipo = TiposDeLancamento.TentarConverter(tipoInformado);

            Assert.Contains(CodigosDeErro.TipoDeLancamentoInvalido, tipo.Erros);
        }

        [Fact]
        public void HashDaRequisicao_DeveSerIgual_QuandoAsPartesForemIguais()
        {
            var primeiroHash = HashDaRequisicao.Calcular("Credito", "10.00", "2026-11-15", "Venda");
            var segundoHash = HashDaRequisicao.Calcular("Credito", "10.00", "2026-11-15", "Venda");

            Assert.True(primeiroHash.IgualA(segundoHash.Valor));
            Assert.Equal(HashDaRequisicao.TamanhoEmCaracteres, primeiroHash.Valor.Length);
        }
    }
}
