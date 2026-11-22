using System;
using Infra.Seguranca;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infra.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class CriarEsquemaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Campanhas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    DataInicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MetaFinanceira = table.Column<decimal>(type: "numeric(31,2)", precision: 31, scale: 2, nullable: false),
                    ValorTotalArrecadado = table.Column<decimal>(type: "numeric(31,2)", precision: 31, scale: 2, nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campanhas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Senha = table.Column<string>(type: "text", nullable: false),
                    Perfil = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaId = table.Column<int>(type: "integer", nullable: false),
                    DoadorId = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(31,2)", precision: 31, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataRecebimento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataProcessamento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doacoes_Campanhas_CampanhaId",
                        column: x => x.CampanhaId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doacoes_Usuarios_DoadorId",
                        column: x => x.DoadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Doacoes_CampanhaId",
                table: "Doacoes",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_Doacoes_DoadorId",
                table: "Doacoes",
                column: "DoadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cpf",
                table: "Usuarios",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            const int statusCampanhaAtiva = 1;
            migrationBuilder.InsertData(
                table: "Campanhas",
                columns: new[] { "Titulo", "Descricao", "DataInicio", "DataFim", "MetaFinanceira", "Status" },
                values: new object[]
                {
                    "Campanha Escola Pública",
                    "Campanha Arrecadação Fundos para Escola Pública",
                    new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2027, 06, 1, 3, 0, 0, TimeSpan.Zero),
                    50_000m,
                    statusCampanhaAtiva
                });

            const int perfilGestorOng = 2;
            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Nome", "Email", "Cpf", "Senha", "Perfil" },
                values: new object[]
                {
                    "Administrador",
                    "admin@admin.com",
                    "00000000000",
                    new HashSenha().Gerar("1234"),
                    perfilGestorOng
                });

            const int perfilDoador = 1;
            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Nome", "Email", "Cpf", "Senha", "Perfil" },
                values: new object[]
                {
                    "João da Silva",
                    "joao@email.com",
                    "00000000001",
                    new HashSenha().Gerar("1234"),
                    perfilDoador
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Doacoes");

            migrationBuilder.DropTable(
                name: "Campanhas");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
