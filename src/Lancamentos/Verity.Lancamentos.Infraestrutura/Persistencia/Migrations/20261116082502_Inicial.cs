using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Verity.Lancamentos.Infraestrutura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lancamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    comerciante_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    data_competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    estorno_de_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chave_de_idempotencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hash_da_requisicao = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    registrado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lancamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_lancamentos_lancamentos_estorno_de_id",
                        column: x => x.estorno_de_id,
                        principalTable: "lancamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_do_evento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    chave_de_roteamento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    contexto_de_rastreamento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ocorrido_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    publicado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    ultimo_erro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lancamentos_comerciante_data_competencia",
                table: "lancamentos",
                columns: new[] { "comerciante_id", "data_competencia" });

            migrationBuilder.CreateIndex(
                name: "ux_lancamentos_comerciante_chave_de_idempotencia",
                table: "lancamentos",
                columns: new[] { "comerciante_id", "chave_de_idempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_lancamentos_estorno_de_id",
                table: "lancamentos",
                column: "estorno_de_id",
                unique: true,
                filter: "estorno_de_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pendentes",
                table: "outbox",
                column: "id",
                filter: "publicado_em_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_outbox_evento_id",
                table: "outbox",
                column: "evento_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lancamentos");

            migrationBuilder.DropTable(
                name: "outbox");
        }
    }
}
