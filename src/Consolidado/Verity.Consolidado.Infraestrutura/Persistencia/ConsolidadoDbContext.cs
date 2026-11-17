using Microsoft.EntityFrameworkCore;
using Verity.Compartilhado.Aplicacao;
using Verity.Consolidado.Core.Dominio.Entidades;
using Verity.Consolidado.Infraestrutura.Persistencia.Configuracoes;

namespace Verity.Consolidado.Infraestrutura.Persistencia
{
    public sealed class ConsolidadoDbContext(
        DbContextOptions<ConsolidadoDbContext> opcoes,
        IComercianteAtual? comercianteAtual = null)
        : DbContext(opcoes)
    {
        public DbSet<SaldoDiario> SaldosDiarios => Set<SaldoDiario>();

        public DbSet<EventoProcessado> EventosProcessados => Set<EventoProcessado>();

        private string IdentificadorDoComercianteDaRequisicao => comercianteAtual?.Identificador ?? string.Empty;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new SaldoDiarioConfiguration());
            modelBuilder.ApplyConfiguration(new EventoProcessadoConfiguration());

            modelBuilder.Entity<SaldoDiario>()
                .HasQueryFilter(saldo => saldo.ComercianteId == IdentificadorDoComercianteDaRequisicao);
        }
    }
}
