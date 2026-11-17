using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Hospedagem.Respostas;

namespace Verity.Compartilhado.Hospedagem.LimiteDeRequisicoes
{
    public static class LimiteDeRequisicoesPorComercianteExtensions
    {
        public const string CodigoDoErroDeLimiteExcedido = "LIMITE_DE_REQUISICOES_EXCEDIDO";

        private const string ParticaoSemLimite = "";

        public static IServiceCollection AdicionarLimiteDeRequisicoesPorComerciante(
            this IServiceCollection servicos,
            IConfiguration configuracao)
        {
            servicos.AddOptions<LimiteDeRequisicoesPorComercianteOptions>()
                .Bind(configuracao.GetSection(LimiteDeRequisicoesPorComercianteOptions.Secao));

            servicos.AddRateLimiter(_ => { });

            servicos.AddOptions<RateLimiterOptions>()
                .Configure<IOptions<LimiteDeRequisicoesPorComercianteOptions>>((opcoesDoLimitador, opcoes) =>
                {
                    opcoesDoLimitador.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    opcoesDoLimitador.OnRejected = ResponderLimiteExcedidoAsync;
                    opcoesDoLimitador.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                        contexto => CriarParticaoDoComerciante(contexto, opcoes.Value));
                });

            return servicos;
        }

        private static RateLimitPartition<string> CriarParticaoDoComerciante(
            HttpContext contexto,
            LimiteDeRequisicoesPorComercianteOptions opcoes)
        {
            var identificadorDoComerciante = contexto.RequestServices.GetService<IComercianteAtual>()?.Identificador;

            if (!opcoes.Habilitado || string.IsNullOrWhiteSpace(identificadorDoComerciante))
                return RateLimitPartition.GetNoLimiter(ParticaoSemLimite);

            return RateLimitPartition.GetTokenBucketLimiter(identificadorDoComerciante, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = opcoes.LimiteDeRajada,
                TokensPerPeriod = opcoes.RequisicoesPorPeriodo,
                ReplenishmentPeriod = opcoes.Periodo,
                QueueLimit = 0,
                AutoReplenishment = true
            });
        }

        private static async ValueTask ResponderLimiteExcedidoAsync(OnRejectedContext rejeicao, CancellationToken cancellationToken)
        {
            var contexto = rejeicao.HttpContext;

            if (rejeicao.Lease.TryGetMetadata(MetadataName.RetryAfter, out var esperaSugerida))
                contexto.Response.Headers.RetryAfter = Math.Ceiling(esperaSugerida.TotalSeconds).ToString(CultureInfo.InvariantCulture);

            var problema = ProblemaDeNegocio.Criar(contexto, StatusCodes.Status429TooManyRequests, [CodigoDoErroDeLimiteExcedido]);

            await contexto.Response.WriteAsJsonAsync(problema, options: null, ProblemaDeNegocio.TipoDeConteudo, cancellationToken);
        }
    }
}
