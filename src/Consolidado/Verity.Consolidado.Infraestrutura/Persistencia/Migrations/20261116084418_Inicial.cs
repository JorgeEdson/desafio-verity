using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Verity.Consolidado.Infraestrutura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evento_processado",
                columns: table => new
                {
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evento_processado", x => x.evento_id);
                });

            migrationBuilder.CreateTable(
                name: "saldo_diario",
                columns: table => new
                {
                    comerciante_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    total_creditos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_debitos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_final = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    atualizado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saldo_diario", x => new { x.comerciante_id, x.data });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evento_processado");

            migrationBuilder.DropTable(
                name: "saldo_diario");
        }
    }
}
