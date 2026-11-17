using System.Net.Http.Json;

namespace Verity.Lancamentos.Testes.Integracao.Infraestrutura
{
    public static class ClienteHttpDeLancamentosExtensions
    {
        private const string CabecalhoDaChaveDeIdempotencia = "Idempotency-Key";

        public static Task<HttpResponseMessage> RegistrarLancamentoAsync(
            this HttpClient clienteHttp,
            object corpoDaRequisicao,
            string? chaveDeIdempotencia)
        {
            var requisicao = new HttpRequestMessage(HttpMethod.Post, "/lancamentos")
            {
                Content = JsonContent.Create(corpoDaRequisicao)
            };

            if (chaveDeIdempotencia is not null)
                requisicao.Headers.Add(CabecalhoDaChaveDeIdempotencia, chaveDeIdempotencia);

            return clienteHttp.SendAsync(requisicao);
        }

        public static Task<HttpResponseMessage> EstornarLancamentoAsync(
            this HttpClient clienteHttp,
            Guid idDoLancamento,
            string chaveDeIdempotencia)
        {
            var requisicao = new HttpRequestMessage(HttpMethod.Post, $"/lancamentos/{idDoLancamento}/estornos");
            requisicao.Headers.Add(CabecalhoDaChaveDeIdempotencia, chaveDeIdempotencia);
            return clienteHttp.SendAsync(requisicao);
        }

        public static object CorpoDeLancamentoValido(decimal valor = 250.00m, string tipo = "Credito") => new
        {
            tipo,
            valor,
            dataCompetencia = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            descricao = "Venda no cartão"
        };
    }
}
