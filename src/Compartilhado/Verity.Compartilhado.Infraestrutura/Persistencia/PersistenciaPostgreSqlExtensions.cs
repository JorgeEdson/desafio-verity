using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Verity.Compartilhado.Saude;

namespace Verity.Compartilhado.Infraestrutura.Persistencia
{
    public static class PersistenciaPostgreSqlExtensions
    {
        public static IServiceCollection AdicionarBancoPostgreSql<TDbContext>(
            this IServiceCollection servicos,
            string nomeDaStringDeConexao,
            params IInterceptor[] interceptadores)
            where TDbContext : DbContext
        {
            servicos.AddDbContext<TDbContext>((provedor, opcoes) => OpcoesDoPostgreSql.Configurar(
                opcoes,
                ObterStringDeConexao(provedor.GetRequiredService<IConfiguration>(), nomeDaStringDeConexao),
                interceptadores));

            return servicos;
        }

        public static IServiceCollection AdicionarVerificacaoDeSaudeDoBanco<TDbContext>(this IServiceCollection servicos)
            where TDbContext : DbContext
        {
            servicos.AddHealthChecks().AddDbContextCheck<TDbContext>(
                typeof(TDbContext).Name,
                tags: [EtiquetasDeVerificacaoDeSaude.DependenciaObrigatoriaParaProntidao]);

            return servicos;
        }

        public static async Task AplicarMigracoesAsync<TDbContext>(this IHost host)
            where TDbContext : DbContext
        {
            await using var escopo = host.Services.CreateAsyncScope();
            var contexto = escopo.ServiceProvider.GetRequiredService<TDbContext>();
            await contexto.Database.MigrateAsync();
        }

        private static string ObterStringDeConexao(IConfiguration configuracao, string nomeDaStringDeConexao) =>
            configuracao.GetConnectionString(nomeDaStringDeConexao)
            ?? throw new InvalidOperationException($"A connection string '{nomeDaStringDeConexao}' não foi configurada.");
    }
}
