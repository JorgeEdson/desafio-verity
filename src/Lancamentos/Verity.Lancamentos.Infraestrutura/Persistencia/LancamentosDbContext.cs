using Microsoft.EntityFrameworkCore;
using Verity.Compartilhado.Aplicacao;
using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Lancamentos.Infraestrutura.Outbox;
using Verity.Lancamentos.Infraestrutura.Persistencia.Configuracoes;

namespace Verity.Lancamentos.Infraestrutura.Persistencia
{
    public sealed class LancamentosDbContext(
        DbContextOptions<LancamentosDbContext> opcoes,
        IComercianteAtual? comercianteAtual = null)
        : DbContext(opcoes)
    {
        public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

        public DbSet<EventoNoOutbox> EventosNoOutbox => Set<EventoNoOutbox>();

        private string IdentificadorDoComercianteDaRequisicao => comercianteAtual?.Identificador ?? string.Empty;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LancamentoConfiguration());
            modelBuilder.ApplyConfiguration(new EventoNoOutboxConfiguration());

            modelBuilder.Entity<Lancamento>()
                .HasQueryFilter(lancamento => lancamento.ComercianteId == IdentificadorDoComercianteDaRequisicao);
        }
    }
}
