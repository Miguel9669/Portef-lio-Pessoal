using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Portfolio.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class TimelineGithub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagemNota");

            migrationBuilder.DropTable(
                name: "Nota");

            migrationBuilder.AddColumn<string>(
                name: "RepositorioGithub",
                table: "Projeto",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projeto_RepositorioGithub",
                table: "Projeto",
                sql: "\"RepositorioGithub\" ~ '^https://github\\.com/[A-Za-z0-9-]{1,39}/[A-Za-z0-9._-]{1,100}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Projeto_RepositorioGithub",
                table: "Projeto");

            migrationBuilder.DropColumn(
                name: "RepositorioGithub",
                table: "Projeto");

            migrationBuilder.CreateTable(
                name: "Nota",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjetoId = table.Column<int>(type: "integer", nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nota", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nota_Projeto_ProjetoId",
                        column: x => x.ProjetoId,
                        principalTable: "Projeto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagemNota",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ficheiro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NotaId = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    TipoMime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagemNota", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagemNota_Nota_NotaId",
                        column: x => x.NotaId,
                        principalTable: "Nota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImagemNota_Ficheiro",
                table: "ImagemNota",
                column: "Ficheiro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagemNota_NotaId",
                table: "ImagemNota",
                column: "NotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Nota_ProjetoId_CriadoEm",
                table: "Nota",
                columns: new[] { "ProjetoId", "CriadoEm" });
        }
    }
}
