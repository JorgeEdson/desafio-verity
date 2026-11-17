using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Lancamentos.Core.Dominio.ObjetosDeValor;

namespace Verity.Lancamentos.Infraestrutura.Persistencia.Configuracoes
{
    public sealed class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
    {
        private const string NomeDaTabela = "lancamentos";
        private const int TamanhoMaximoDoTipo = 10;
        private const int PrecisaoDoValor = 18;
        private const int EscalaDoValor = 2;
        private const string FiltroDeLancamentosQueSaoEstornos = "estorno_de_id IS NOT NULL";

        public void Configure(EntityTypeBuilder<Lancamento> builder)
        {
            builder.ToTable(NomeDaTabela);

            builder.HasKey(lancamento => lancamento.Id);

            builder.Property(lancamento => lancamento.Id)
                .ValueGeneratedNever();

            builder.Property(lancamento => lancamento.ComercianteId)
                .HasMaxLength(IdentificadorDoComerciante.TamanhoMaximo)
                .IsRequired();

            builder.Property(lancamento => lancamento.Tipo)
                .HasConversion<string>()
                .HasMaxLength(TamanhoMaximoDoTipo)
                .IsRequired();

            builder.Property(lancamento => lancamento.Valor)
                .HasPrecision(PrecisaoDoValor, EscalaDoValor);

            builder.Property(lancamento => lancamento.DataCompetencia);

            builder.Property(lancamento => lancamento.Descricao)
                .HasMaxLength(DescricaoDoLancamento.TamanhoMaximo)
                .IsRequired();

            builder.Property(lancamento => lancamento.ChaveDeIdempotencia)
                .HasMaxLength(ChaveDeIdempotencia.TamanhoMaximo)
                .IsRequired();

            builder.Property(lancamento => lancamento.HashDaRequisicao)
                .HasMaxLength(HashDaRequisicao.TamanhoEmCaracteres)
                .IsFixedLength()
                .IsRequired();

            builder.Property(lancamento => lancamento.RegistradoEmUtc);

            builder.Ignore(lancamento => lancamento.EhEstorno);
            builder.Ignore(lancamento => lancamento.EventosDeDominio);

            builder.HasOne<Lancamento>()
                .WithMany()
                .HasForeignKey(lancamento => lancamento.EstornoDeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(lancamento => new { lancamento.ComercianteId, lancamento.ChaveDeIdempotencia })
                .IsUnique()
                .HasDatabaseName(NomesDeRestricoesDoBanco.ChaveDeIdempotenciaUnicaPorComerciante);

            builder.HasIndex(lancamento => lancamento.EstornoDeId)
                .IsUnique()
                .HasFilter(FiltroDeLancamentosQueSaoEstornos)
                .HasDatabaseName(NomesDeRestricoesDoBanco.EstornoUnicoPorLancamento);

            builder.HasIndex(lancamento => new { lancamento.ComercianteId, lancamento.DataCompetencia })
                .HasDatabaseName(NomesDeRestricoesDoBanco.LancamentosPorComercianteEDataDeCompetencia);
        }
    }
}
