using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Verity.Consolidado.Infraestrutura.Persistencia.Configuracoes
{
    public sealed class EventoProcessadoConfiguration : IEntityTypeConfiguration<EventoProcessado>
    {
        private const string NomeDaTabela = "evento_processado";

        public void Configure(EntityTypeBuilder<EventoProcessado> builder)
        {
            builder.ToTable(NomeDaTabela);

            builder.HasKey(evento => evento.EventoId);

            builder.Property(evento => evento.EventoId)
                .ValueGeneratedNever();

            builder.Property(evento => evento.ProcessadoEmUtc);
        }
    }
}
