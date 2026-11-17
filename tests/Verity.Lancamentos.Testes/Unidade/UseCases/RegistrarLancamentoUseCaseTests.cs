using NSubstitute;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Contratos;
using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.UseCases.RegistrarLancamento;

namespace Verity.Lancamentos.Testes.Unidade.UseCases
{
    public sealed class RegistrarLancamentoUseCaseTests
    {
        private static readonly DateOnly DataDeHojeDoComerciante = new(2026, 11, 16);
        private static readonly DateTime InstanteAtualUtc = new(2026, 11, 16, 13, 0, 0, DateTimeKind.Utc);

        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly ILancamentoRepository _lancamentos = Substitute.For<ILancamentoRepository>();
        private readonly IComercianteAtual _comercianteAtual = Substitute.For<IComercianteAtual>();
        private readonly IRelogioDoComerciante _relogioDoComerciante = Substitute.For<IRelogioDoComerciante>();
        private readonly RegistrarLancamentoUseCase _useCase;

        public RegistrarLancamentoUseCaseTests()
        {
            _unitOfWork.Lancamentos.Returns(_lancamentos);
            _unitOfWork.ConfirmarAlteracoesAsync(Arg.Any<CancellationToken>()).Returns(Resultado.Sucesso());
            _comercianteAtual.Identificador.Returns("c-001");
            _relogioDoComerciante.ObterDataDeHoje().Returns(DataDeHojeDoComerciante);
            _relogioDoComerciante.ObterInstanteAtualUtc().Returns(InstanteAtualUtc);

            _useCase = new RegistrarLancamentoUseCase(_unitOfWork, _comercianteAtual, _relogioDoComerciante);
        }

        [Fact]
        public async Task ExecutarAsync_DeveGravarERetornarLancamentoCriado_QuandoChaveDeIdempotenciaForNova()
        {
            var resultado = await _useCase.ExecutarAsync(EntradaValida("chave-nova"));

            Assert.True(resultado.EhSucesso);
            Assert.True(resultado.Instancia.FoiCriadoNestaRequisicao);
            _lancamentos.Received(1).Adicionar(Arg.Any<Lancamento>());
            await _unitOfWork.Received(1).ConfirmarAlteracoesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveFalhar_QuandoComercianteNaoEstiverIdentificado()
        {
            _comercianteAtual.Identificador.Returns(string.Empty);

            var resultado = await _useCase.ExecutarAsync(EntradaValida("chave"));

            Assert.Contains(CodigosDeErro.ComercianteNaoIdentificado, resultado.Erros);
            _lancamentos.DidNotReceive().Adicionar(Arg.Any<Lancamento>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveReunirTodosOsErrosDeValidacao_QuandoEntradaForInvalida()
        {
            var entradaInvalida = new RegistrarLancamentoEntrada("Transferencia", -1m, DataDeHojeDoComerciante.AddDays(1), " ", " ");

            var resultado = await _useCase.ExecutarAsync(entradaInvalida);

            Assert.Contains(CodigosDeErro.TipoDeLancamentoInvalido, resultado.Erros);
            Assert.Contains(CodigosDeErro.ValorDoLancamentoDeveSerPositivo, resultado.Erros);
            Assert.Contains(CodigosDeErro.DataDeCompetenciaNoFuturo, resultado.Erros);
            Assert.Contains(CodigosDeErro.DescricaoObrigatoria, resultado.Erros);
            Assert.Contains(CodigosDeErro.ChaveDeIdempotenciaObrigatoria, resultado.Erros);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarOLancamentoExistenteSemGravarNovamente_QuandoAMesmaRequisicaoForRepetida()
        {
            var entrada = EntradaValida("chave-repetida");
            var lancamentoGravado = await ExecutarECapturarLancamentoGravadoAsync(entrada);
            _lancamentos.ObterPorChaveDeIdempotenciaAsync(entrada.ChaveDeIdempotencia, Arg.Any<CancellationToken>()).Returns(lancamentoGravado);
            _lancamentos.ClearReceivedCalls();

            var repeticao = await _useCase.ExecutarAsync(entrada);

            Assert.True(repeticao.EhSucesso);
            Assert.False(repeticao.Instancia.FoiCriadoNestaRequisicao);
            Assert.Equal(lancamentoGravado.Id, repeticao.Instancia.Lancamento.Id);
            _lancamentos.DidNotReceive().Adicionar(Arg.Any<Lancamento>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveFalhar_QuandoAChaveForReutilizadaComConteudoDiferente()
        {
            var entradaOriginal = EntradaValida("chave-reutilizada");
            var lancamentoGravado = await ExecutarECapturarLancamentoGravadoAsync(entradaOriginal);
            _lancamentos.ObterPorChaveDeIdempotenciaAsync(entradaOriginal.ChaveDeIdempotencia, Arg.Any<CancellationToken>()).Returns(lancamentoGravado);

            var resultado = await _useCase.ExecutarAsync(entradaOriginal with { Valor = 999.99m });

            Assert.Contains(CodigosDeErro.ChaveDeIdempotenciaReutilizadaComConteudoDiferente, resultado.Erros);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarOLancamentoDaRequisicaoConcorrente_QuandoAGravacaoViolarAUnicidadeDaChave()
        {
            var entrada = EntradaValida("chave-concorrente");
            var lancamentoDaRequisicaoConcorrente = await ExecutarECapturarLancamentoGravadoAsync(entrada);

            _lancamentos.ObterPorChaveDeIdempotenciaAsync(entrada.ChaveDeIdempotencia, Arg.Any<CancellationToken>())
                .Returns((Lancamento?)null, lancamentoDaRequisicaoConcorrente);
            _unitOfWork.ConfirmarAlteracoesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado.Falha(CodigosDeErro.ChaveDeIdempotenciaJaUtilizada));

            var resultado = await _useCase.ExecutarAsync(entrada);

            Assert.True(resultado.EhSucesso);
            Assert.False(resultado.Instancia.FoiCriadoNestaRequisicao);
            Assert.Equal(lancamentoDaRequisicaoConcorrente.Id, resultado.Instancia.Lancamento.Id);
        }

        private static RegistrarLancamentoEntrada EntradaValida(string chaveDeIdempotencia) => new(
            "Credito",
            8000.00m,
            DataDeHojeDoComerciante,
            "Recebimento de salário",
            chaveDeIdempotencia);

        private async Task<Lancamento> ExecutarECapturarLancamentoGravadoAsync(RegistrarLancamentoEntrada entrada)
        {
            Lancamento? lancamentoGravado = null;
            _lancamentos.When(repositorio => repositorio.Adicionar(Arg.Any<Lancamento>()))
                .Do(chamada => lancamentoGravado = chamada.Arg<Lancamento>());

            await _useCase.ExecutarAsync(entrada);

            return lancamentoGravado ?? throw new InvalidOperationException("O use case não gravou o lançamento.");
        }
    }
}
