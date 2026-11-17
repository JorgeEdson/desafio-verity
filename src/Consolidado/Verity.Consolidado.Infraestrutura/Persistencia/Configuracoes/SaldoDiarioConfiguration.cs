using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verity.Consolidado.Core.Dominio.Entidades;

namespace Verity.Consolidado.Infraestrutura.Persistencia.Configuracoes
{
    public sealed class SaldoDiarioConfiguration : IEntityTypeConfiguration<SaldoDiario>
    {
        private const string NomeDaTabela = "saldo_diario";
        private const int TamanhoMaximoDoIdentificadorDoComerciante = 64;
        private const int PrecisaoDosValores = 18;
        private const int EscalaDosValores = 2;

        public void Configure(EntityTypeBuilder<SaldoDiario> builder)
        {
            builder.ToTable(NomeDaTabela);

            builder.HasKey(saldo => new { saldo.ComercianteId, saldo.Data });

            builder.Property(saldo => saldo.ComercianteId)
                .HasMaxLength(TamanhoMaximoDoIdentificadorDoComerciante);

            builder.Property(saldo => saldo.TotalCreditos)
                .HasPrecision(PrecisaoDosValores, EscalaDosValores);

            builder.Property(saldo => saldo.TotalDebitos)
                .HasPrecision(PrecisaoDosValores, EscalaDosValores);

            builder.Property(saldo => saldo.SaldoFinal)
                .HasPrecision(PrecisaoDosValores, EscalaDosValores);

            builder.Property(saldo => saldo.AtualizadoEmUtc);

            builder.Ignore(saldo => saldo.SaldoInicial);
        }
    }
}
