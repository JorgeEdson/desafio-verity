using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verity.Lancamentos.Infraestrutura.Outbox;

namespace Verity.Lancamentos.Infraestrutura.Persistencia.Configuracoes
{
    public sealed class EventoNoOutboxConfiguration : IEntityTypeConfiguration<EventoNoOutbox>
    {
        private const string NomeDaTabela = "outbox";
        private const string TipoDeColunaJson = "jsonb";
        private const int TamanhoMaximoDosIdentificadoresTextuais = 100;
        private const string FiltroDeEventosPendentes = "publicado_em_utc IS NULL";

        public void Configure(EntityTypeBuilder<EventoNoOutbox> builder)
        {
            builder.ToTable(NomeDaTabela);

            builder.HasKey(evento => evento.Id);

            builder.Property(evento => evento.Id)
                .UseIdentityAlwaysColumn();

            builder.Property(evento => evento.TipoDoEvento)
                .HasMaxLength(TamanhoMaximoDosIdentificadoresTextuais)
                .IsRequired();

            builder.Property(evento => evento.ChaveDeRoteamento)
                .HasMaxLength(TamanhoMaximoDosIdentificadoresTextuais)
                .IsRequired();

            builder.Property(evento => evento.Conteudo)
                .HasColumnType(TipoDeColunaJson)
                .IsRequired();

            builder.Property(evento => evento.ContextoDeRastreamento)
                .HasMaxLength(TamanhoMaximoDosIdentificadoresTextuais);

            builder.Property(evento => evento.UltimoErro)
                .HasMaxLength(EventoNoOutbox.TamanhoMaximoDaMensagemDeErro);

            builder.HasIndex(evento => evento.EventoId)
                .IsUnique()
                .HasDatabaseName(NomesDeRestricoesDoBanco.EventoUnicoNoOutbox);

            builder.HasIndex(evento => evento.Id)
                .HasFilter(FiltroDeEventosPendentes)
                .HasDatabaseName(NomesDeRestricoesDoBanco.EventosPendentesNoOutbox);
        }
    }
}
