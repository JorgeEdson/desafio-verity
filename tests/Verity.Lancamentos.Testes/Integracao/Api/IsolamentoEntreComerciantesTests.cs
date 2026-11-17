using System.Net;
using System.Net.Http.Json;
using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.UseCases.Compartilhado;
using Verity.Lancamentos.Testes.Integracao.Infraestrutura;

namespace Verity.Lancamentos.Testes.Integracao.Api
{
    [Collection(ColecaoDoAmbienteDeLancamentos.Nome)]
    public sealed class IsolamentoEntreComerciantesTests(AmbienteDeLancamentosFixture ambiente)
    {
        [Fact]
        public async Task ConsultaPorId_DeveRetornarNotFound_QuandoLancamentoPertencerAOutroComerciante()
        {
            using var clienteDoDono = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            using var clienteDeOutroComerciante = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            var respostaDoRegistro = await clienteDoDono.RegistrarLancamentoAsync(
                ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(),
                Guid.NewGuid().ToString());
            var lancamento = await respostaDoRegistro.Content.ReadFromJsonAsync<LancamentoSaida>();

            var respostaDoDono = await clienteDoDono.GetAsync($"/lancamentos/{lancamento!.Id}");
            var respostaDeOutroComerciante = await clienteDeOutroComerciante.GetAsync($"/lancamentos/{lancamento.Id}");

            Assert.Equal(HttpStatusCode.OK, respostaDoDono.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, respostaDeOutroComerciante.StatusCode);
        }

        [Fact]
        public async Task EstornoDeLancamentoDeOutroComerciante_DeveRetornarNotFound()
        {
            using var clienteDoDono = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            using var clienteDeOutroComerciante = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            var respostaDoRegistro = await clienteDoDono.RegistrarLancamentoAsync(
                ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(),
                Guid.NewGuid().ToString());
            var lancamento = await respostaDoRegistro.Content.ReadFromJsonAsync<LancamentoSaida>();

            var resposta = await clienteDeOutroComerciante.EstornarLancamentoAsync(lancamento!.Id, Guid.NewGuid().ToString());

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        [Fact]
        public async Task Extrato_DeveConterSomenteLancamentosDoComerciante()
        {
            var comerciante = $"c-{Guid.NewGuid():N}";
            using var clienteDoComerciante = ambiente.Api.CriarClienteDoComerciante(comerciante);
            using var clienteDeOutroComerciante = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            await clienteDoComerciante.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(), Guid.NewGuid().ToString());
            await clienteDoComerciante.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(), Guid.NewGuid().ToString());
            await clienteDeOutroComerciante.RegistrarLancamentoAsync(ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(), Guid.NewGuid().ToString());
            var ontem = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

            var extrato = await clienteDoComerciante.GetFromJsonAsync<ResultadoPaginado<LancamentoSaida>>(
                $"/lancamentos?inicio={ontem:yyyy-MM-dd}&fim={ontem:yyyy-MM-dd}");

            Assert.Equal(2, extrato!.TotalDeItens);
        }
    }
}
