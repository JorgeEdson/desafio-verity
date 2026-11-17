using Microsoft.EntityFrameworkCore;
using Verity.Compartilhado.Infraestrutura.Persistencia;

namespace Verity.Consolidado.Infraestrutura.Persistencia
{
    public sealed class ConsolidadoDbContextDesignTimeFactory : PostgreSqlDesignTimeDbContextFactory<ConsolidadoDbContext>
    {
        protected override string StringDeConexaoParaGeracaoDeMigracoes =>
            "Host=localhost;Port=5432;Database=consolidado_db;Username=consolidado;Password=consolidado";

        protected override ConsolidadoDbContext CriarContexto(DbContextOptions<ConsolidadoDbContext> opcoes) => new(opcoes);
    }
}
