using Microsoft.EntityFrameworkCore;
using Verity.Compartilhado.Infraestrutura.Persistencia;

namespace Verity.Lancamentos.Infraestrutura.Persistencia
{
    public sealed class LancamentosDbContextDesignTimeFactory : PostgreSqlDesignTimeDbContextFactory<LancamentosDbContext>
    {
        protected override string StringDeConexaoParaGeracaoDeMigracoes =>
            "Host=localhost;Port=5432;Database=lancamentos_db;Username=lancamentos;Password=lancamentos";

        protected override LancamentosDbContext CriarContexto(DbContextOptions<LancamentosDbContext> opcoes) => new(opcoes);
    }
}
