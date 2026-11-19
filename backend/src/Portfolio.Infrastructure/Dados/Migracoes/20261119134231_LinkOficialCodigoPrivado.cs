using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class LinkOficialCodigoPrivado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CodigoPrivado",
                table: "Projeto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HistoricoPublico",
                table: "Projeto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LinkOficial",
                table: "Projeto",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projeto_LinkOficial",
                table: "Projeto",
                sql: "\"LinkOficial\" ~ '^https://[A-Za-z0-9.-]+\\.[A-Za-z0-9-]+(:[0-9]{1,5})?(/\\S*)?$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Projeto_LinkOficial",
                table: "Projeto");

            migrationBuilder.DropColumn(
                name: "CodigoPrivado",
                table: "Projeto");

            migrationBuilder.DropColumn(
                name: "HistoricoPublico",
                table: "Projeto");

            migrationBuilder.DropColumn(
                name: "LinkOficial",
                table: "Projeto");
        }
    }
}
