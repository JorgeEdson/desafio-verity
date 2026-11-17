using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Verity.Compartilhado.Infraestrutura.Persistencia
{
    public abstract class PostgreSqlDesignTimeDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
        where TDbContext : DbContext
    {
        protected abstract string StringDeConexaoParaGeracaoDeMigracoes { get; }

        public TDbContext CreateDbContext(string[] args) =>
            CriarContexto(OpcoesDoPostgreSql.Criar<TDbContext>(StringDeConexaoParaGeracaoDeMigracoes));

        protected abstract TDbContext CriarContexto(DbContextOptions<TDbContext> opcoes);
    }
}
