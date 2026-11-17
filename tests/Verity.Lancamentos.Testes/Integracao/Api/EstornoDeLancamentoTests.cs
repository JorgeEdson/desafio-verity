using System.Net;
using System.Net.Http.Json;
using Verity.Lancamentos.Core.UseCases.Compartilhado;
using Verity.Lancamentos.Testes.Integracao.Infraestrutura;

namespace Verity.Lancamentos.Testes.Integracao.Api
{
    [Collection(ColecaoDoAmbienteDeLancamentos.Nome)]
    public sealed class EstornoDeLancamentoTests(AmbienteDeLancamentosFixture ambiente)
    {
        [Fact]
        public async Task Estorno_DeveRetornarCreatedComLancamentoDeTipoInverso()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            var original = await RegistrarLancamentoAsync(clienteHttp, tipo: "Credito");

            var resposta = await clienteHttp.EstornarLancamentoAsync(original.Id, Guid.NewGuid().ToString());

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            var estorno = await resposta.Content.ReadFromJsonAsync<LancamentoSaida>();
            Assert.Equal("Debito", estorno!.Tipo);
            Assert.Equal(original.Valor, estorno.Valor);
            Assert.Equal(original.Id, estorno.EstornoDeId);
        }

        [Fact]
        public async Task SegundoEstornoComOutraChave_DeveRetornarConflict()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            var original = await RegistrarLancamentoAsync(clienteHttp, tipo: "Debito");

            await clienteHttp.EstornarLancamentoAsync(original.Id, Guid.NewGuid().ToString());
            var segundoEstorno = await clienteHttp.EstornarLancamentoAsync(original.Id, Guid.NewGuid().ToString());

            Assert.Equal(HttpStatusCode.Conflict, segundoEstorno.StatusCode);
        }

        [Fact]
        public async Task EstornoRepetidoComAMesmaChave_DeveRetornarOkComOMesmoEstorno()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            var original = await RegistrarLancamentoAsync(clienteHttp, tipo: "Credito");
            var chave = Guid.NewGuid().ToString();

            var primeiraResposta = await clienteHttp.EstornarLancamentoAsync(original.Id, chave);
            var segundaResposta = await clienteHttp.EstornarLancamentoAsync(original.Id, chave);

            Assert.Equal(HttpStatusCode.Created, primeiraResposta.StatusCode);
            Assert.Equal(HttpStatusCode.OK, segundaResposta.StatusCode);
        }

        [Fact]
        public async Task EstornoDeUmEstorno_DeveRetornarConflict()
        {
            using var clienteHttp = ambiente.Api.CriarClienteDoComerciante($"c-{Guid.NewGuid():N}");
            var original = await RegistrarLancamentoAsync(clienteHttp, tipo: "Credito");
            var respostaDoEstorno = await clienteHttp.EstornarLancamentoAsync(original.Id, Guid.NewGuid().ToString());
            var estorno = await respostaDoEstorno.Content.ReadFromJsonAsync<LancamentoSaida>();

            var resposta = await clienteHttp.EstornarLancamentoAsync(estorno!.Id, Guid.NewGuid().ToString());

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        private static async Task<LancamentoSaida> RegistrarLancamentoAsync(HttpClient clienteHttp, string tipo)
        {
            var resposta = await clienteHttp.RegistrarLancamentoAsync(
                ClienteHttpDeLancamentosExtensions.CorpoDeLancamentoValido(tipo: tipo),
                Guid.NewGuid().ToString());

            return (await resposta.Content.ReadFromJsonAsync<LancamentoSaida>())!;
        }
    }
}
