using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Verity.Compartilhado.Infraestrutura.Persistencia
{
    public static class OpcoesDoPostgreSql
    {
        public static DbContextOptionsBuilder Configurar(
            DbContextOptionsBuilder opcoes,
            string stringDeConexao,
            params IInterceptor[] interceptadores) =>
            opcoes
                .UseNpgsql(stringDeConexao)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(interceptadores);

        public static DbContextOptions<TDbContext> Criar<TDbContext>(
            string stringDeConexao,
            params IInterceptor[] interceptadores)
            where TDbContext : DbContext
        {
            var opcoes = new DbContextOptionsBuilder<TDbContext>();
            Configurar(opcoes, stringDeConexao, interceptadores);
            return opcoes.Options;
        }
    }
}
