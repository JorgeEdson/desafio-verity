using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Verity.Gateway.Testes.Infraestrutura
{
    internal sealed class ApiDeDestinoFake : IAsyncDisposable
    {
        private const string EnderecoComPortaAleatoria = "http://127.0.0.1:0";

        private readonly WebApplication _aplicacao;

        private ApiDeDestinoFake(string nome, WebApplication aplicacao)
        {
            Nome = nome;
            _aplicacao = aplicacao;
        }

        public string Nome { get; }

        public string Endereco => _aplicacao.Urls.First() + "/";

        public ConcurrentQueue<string> CaminhosRecebidos { get; } = new();

        public ConcurrentQueue<string?> CabecalhosAuthorizationRecebidos { get; } = new();

        public static async Task<ApiDeDestinoFake> IniciarAsync(string nome)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls(EnderecoComPortaAleatoria);
            builder.Logging.ClearProviders();

            var aplicacao = builder.Build();
            var apiDeDestino = new ApiDeDestinoFake(nome, aplicacao);

            aplicacao.MapGet("/health/ready", () => Results.Ok());
            aplicacao.Map("/{**caminho}", (HttpContext contexto) => apiDeDestino.RegistrarRequisicaoRecebida(contexto));

            await aplicacao.StartAsync();
            return apiDeDestino;
        }

        public async ValueTask DisposeAsync()
        {
            await _aplicacao.StopAsync();
            await _aplicacao.DisposeAsync();
        }

        private IResult RegistrarRequisicaoRecebida(HttpContext contexto)
        {
            CaminhosRecebidos.Enqueue(contexto.Request.Path + contexto.Request.QueryString);
            CabecalhosAuthorizationRecebidos.Enqueue(contexto.Request.Headers.Authorization.FirstOrDefault());
            return Results.Text(Nome);
        }
    }
}
