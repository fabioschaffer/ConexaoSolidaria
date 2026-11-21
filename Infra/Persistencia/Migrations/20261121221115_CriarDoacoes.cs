using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infra.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class CriarDoacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Doacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampanhaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(31,2)", precision: 31, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataRecebimento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataProcessamento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doacoes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Doacoes");
        }
    }
}
