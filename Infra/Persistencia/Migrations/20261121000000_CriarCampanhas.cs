using Microsoft.EntityFrameworkCore.Migrations;

namespace Infra.Persistencia.Migrations;

public partial class CriarCampanhas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Campanhas",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Titulo = table.Column<string>(type: "text", nullable: false),
                Descricao = table.Column<string>(type: "text", nullable: false),
                DataInicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DataFim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                MetaFinanceira = table.Column<decimal>(type: "numeric(31,2)", precision: 31, scale: 2, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Campanhas", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable("Campanhas");
}
