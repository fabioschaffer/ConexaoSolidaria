using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infra.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class RenomearNomeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NomeCompleto",
                table: "Usuarios",
                newName: "Nome");

            migrationBuilder.Sql("""UPDATE "Usuarios" SET "Nome" = '';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Usuarios",
                newName: "NomeCompleto");
        }
    }
}
