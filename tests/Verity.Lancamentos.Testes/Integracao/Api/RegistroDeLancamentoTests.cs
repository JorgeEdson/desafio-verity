using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Verity.Lancamentos.Core.UseCases.Compartilhado;
using Verity.Lancamentos.Testes.Compartilhado;
using Verity.Lancamentos.Testes.Integracao.Infraestrutura;

namespace Verity.Lancamentos.Testes.Integracao.Api
{
    [Collection(ColecaoDoAmbienteDeLancamentos.Nome)]
    public sealed class RegistroDeLancamentoTests(AmbienteDeLancamentosFixture ambiente)
    {
        private const int QuantidadeDeRequisicoesSimultaneas = 10;

        [Fact]
        public async Task Post_DeveRetornarCreatedEGravarEventoPendenteNoOutbox_SemDependerDoRabbitMq()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(NovoComerciante());

            var resposta = await clienteHttp.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(), NovaChave());

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            var lancamento = await resposta.Content.ReadFromJsonAsync<LancamentoSaida>();
            Assert.Equal($"/lancamentos/{lancamento!.Id}", resposta.Headers.Location?.OriginalString);

            var eventoNoOutbox = await ambiente.ObterEventoDoOutboxDoLancamentoAsync(lancamento.Id);
            Assert.NotNull(eventoNoOutbox);
            Assert.Null(eventoNoOutbox.PublicadoEmUtc);
        }

        [Fact]
        public async Task PostRepetidoComAMesmaChave_DeveRetornarOkComOMesmoLancamento()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(NovoComerciante());
            var corpo = ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido();
            var chave = NovaChave();

            var primeiraResposta = await clienteHttp.RegistrarLancamentoAsync(corpo, chave);
            var segundaResposta = await clienteHttp.RegistrarLancamentoAsync(corpo, chave);

            Assert.Equal(HttpStatusCode.Created, primeiraResposta.StatusCode);
            Assert.Equal(HttpStatusCode.OK, segundaResposta.StatusCode);
            var primeiroLancamento = await primeiraResposta.Content.ReadFromJsonAsync<LancamentoSaida>();
            var segundoLancamento = await segundaResposta.Content.ReadFromJsonAsync<LancamentoSaida>();
            Assert.Equal(primeiroLancamento!.Id, segundoLancamento!.Id);
        }

        [Fact]
        public async Task PostsSimultaneosComAMesmaChave_DevemGravarUmUnicoLancamento()
        {
            var comerciante = NovoComerciante();
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(comerciante);
            var corpo = ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido();
            var chave = NovaChave();

            var respostas = await Task.WhenAll(Enumerable
                .Range(0, QuantidadeDeRequisicoesSimultaneas)
                .Select(_ => clienteHttp.RegistrarLancamentoAsync(corpo, chave)));

            Assert.All(respostas, resposta => Assert.True(resposta.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK));
            Assert.Single(respostas, resposta => resposta.StatusCode == HttpStatusCode.Created);

            await using var contexto = ambiente.CriarContexto(new ComercianteAtualFake(comerciante));
            Assert.Equal(1, await contexto.Lancamentos.CountAsync());
        }

        [Fact]
        public async Task PostComChaveReutilizadaEConteudoDiferente_DeveRetornarUnprocessableEntity()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(NovoComerciante());
            var chave = NovaChave();

            await clienteHttp.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(valor: 100.00m), chave);
            var resposta = await clienteHttp.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(valor: 200.00m), chave);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        }

        [Fact]
        public async Task PostSemChaveDeIdempotencia_DeveRetornarBadRequest()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(NovoComerciante());

            var resposta = await clienteHttp.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(), chaveDeIdempotencia: null);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task PostComDataDeCompetenciaFutura_DeveRetornarBadRequest()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante(NovoComerciante());
            var corpoComDataFutura = new
            {
                tipo = "Debito",
                valor = 10.00m,
                dataCompetencia = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                descricao = "Conta de luz"
            };

            var resposta = await clienteHttp.RegistrarLancamentoAsync(corpoComDataFutura, NovaChave());

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task PostSemToken_DeveRetornarUnauthorized()
        {
            using var clienteHttp = ambiente.Api.CreateClient();

            var resposta = await clienteHttp.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(), NovaChave());

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task PostComTokenSemComerciante_DeveRetornarForbidden()
        {
            using var clienteHttp = ambiente.Api.CriarClienteComToken(EmissorDeTokensDeTeste.EmitirTokenSemComerciante());

            var resposta = await clienteHttp.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(), NovaChave());

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        private static string NovoComerciante() => $"c-{Guid.NewGuid():N}";

        private static string NovaChave() => Guid.NewGuid().ToString();
    }
}
