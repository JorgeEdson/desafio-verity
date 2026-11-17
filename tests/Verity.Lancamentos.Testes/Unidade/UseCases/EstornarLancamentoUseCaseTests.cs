using NSubstitute;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Lancamentos.Core.Dominio.Enumeradores;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.UseCases.EstornarLancamento;
using Verity.Lancamentos.Testes.Compartilhado;

namespace Verity.Lancamentos.Testes.Unidade.UseCases
{
    public sealed class EstornarLancamentoUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly ILancamentoRepository _lancamentos = Substitute.For<ILancamentoRepository>();
        private readonly IComercianteAtual _comercianteAtual = Substitute.For<IComercianteAtual>();
        private readonly IRelogioDoComerciante _relogioDoComerciante = Substitute.For<IRelogioDoComerciante>();
        private readonly EstornarLancamentoUseCase _useCase;

        public EstornarLancamentoUseCaseTests()
        {
            _unitOfWork.Lancamentos.Returns(_lancamentos);
            _unitOfWork.ConfirmarAlteracoesAsync(Arg.Any<CancellationToken>()).Returns(Resultado.Sucesso());
            _comercianteAtual.Identificador.Returns("c-teste");
            _relogioDoComerciante.ObterInstanteAtualUtc().Returns(new DateTime(2026, 11, 16, 13, 0, 0, DateTimeKind.Utc));

            _useCase = new EstornarLancamentoUseCase(_unitOfWork, _comercianteAtual, _relogioDoComerciante);
        }

        [Fact]
        public async Task ExecutarAsync_DeveGravarEstornoDeTipoInverso_QuandoLancamentoExistirENaoTiverSidoEstornado()
        {
            var original = new LancamentoBuilder().DoTipo(TipoDeLancamento.Credito).Construir();
            _lancamentos.ObterPorIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);

            var resultado = await _useCase.ExecutarAsync(new EstornarLancamentoEntrada(original.Id, "chave-do-estorno"));

            Assert.True(resultado.Instancia.FoiCriadoNestaRequisicao);
            Assert.Equal(nameof(TipoDeLancamento.Debito), resultado.Instancia.Lancamento.Tipo);
            Assert.Equal(original.Id, resultado.Instancia.Lancamento.EstornoDeId);
        }

        [Fact]
        public async Task ExecutarAsync_DeveFalharComNaoEncontrado_QuandoLancamentoNaoExistirParaOComerciante()
        {
            var resultado = await _useCase.ExecutarAsync(new EstornarLancamentoEntrada(Guid.NewGuid(), "chave"));

            Assert.Contains(CodigosDeErro.LancamentoNaoEncontrado, resultado.Erros);
        }

        [Fact]
        public async Task ExecutarAsync_DeveFalharComJaEstornado_QuandoLancamentoJaPossuirEstorno()
        {
            var original = new LancamentoBuilder().Construir();
            _lancamentos.ObterPorIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);
            _lancamentos.ExisteEstornoDoLancamentoAsync(original.Id, Arg.Any<CancellationToken>()).Returns(true);

            var resultado = await _useCase.ExecutarAsync(new EstornarLancamentoEntrada(original.Id, "outra-chave"));

            Assert.Contains(CodigosDeErro.LancamentoJaEstornado, resultado.Erros);
            _lancamentos.DidNotReceive().Adicionar(Arg.Any<Lancamento>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveFalhar_QuandoChaveDeIdempotenciaNaoForInformada()
        {
            var resultado = await _useCase.ExecutarAsync(new EstornarLancamentoEntrada(Guid.NewGuid(), string.Empty));

            Assert.Contains(CodigosDeErro.ChaveDeIdempotenciaObrigatoria, resultado.Erros);
        }
    }
}
