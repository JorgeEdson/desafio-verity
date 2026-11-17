using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Verity.Compartilhado.Infraestrutura.Mensageria
{
    public static class MensageriaRabbitMqExtensions
    {
        public static IServiceCollection AdicionarConexaoRabbitMq(this IServiceCollection servicos, IConfiguration configuracao)
        {
            servicos.AddOptions<ConexaoRabbitMqOptions>().Bind(configuracao.GetSection(ConexaoRabbitMqOptions.Secao));
            return servicos;
        }
    }
}
