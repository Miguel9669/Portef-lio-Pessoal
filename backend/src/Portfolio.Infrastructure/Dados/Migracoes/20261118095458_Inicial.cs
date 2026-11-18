using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Portfolio.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "BlocoConteudo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Conteudo = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Publico = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlocoConteudo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projeto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Publico = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projeto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilizador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "citext", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SecurityStamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TotpSegredoCifrado = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TotpAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    TotpUltimoPasso = table.Column<long>(type: "bigint", nullable: true),
                    TentativasFalhadas = table.Column<int>(type: "integer", nullable: false),
                    BloqueadoAte = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilizador", x => x.Id);
                    table.CheckConstraint("CK_Utilizador_Unico", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "ImagemProjeto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ProjetoId = table.Column<int>(type: "integer", nullable: false),
                    Ficheiro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TipoMime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagemProjeto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagemProjeto_Projeto_ProjetoId",
                        column: x => x.ProjetoId,
                        principalTable: "Projeto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nota",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ProjetoId = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "CodigoRecuperacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UtilizadorId = table.Column<int>(type: "integer", nullable: false),
                    CodigoHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UsadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigoRecuperacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodigoRecuperacao_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagemNota",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    NotaId = table.Column<int>(type: "integer", nullable: false),
                    Ficheiro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TipoMime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_BlocoConteudo_Publico_Ordem",
                table: "BlocoConteudo",
                columns: new[] { "Publico", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_BlocoConteudo_Slug",
                table: "BlocoConteudo",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodigoRecuperacao_UtilizadorId",
                table: "CodigoRecuperacao",
                column: "UtilizadorId");

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
                name: "IX_ImagemProjeto_Ficheiro",
                table: "ImagemProjeto",
                column: "Ficheiro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagemProjeto_ProjetoId",
                table: "ImagemProjeto",
                column: "ProjetoId");

            migrationBuilder.CreateIndex(
                name: "IX_Nota_ProjetoId_CriadoEm",
                table: "Nota",
                columns: new[] { "ProjetoId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Projeto_Publico",
                table: "Projeto",
                column: "Publico");

            migrationBuilder.CreateIndex(
                name: "IX_Projeto_Slug",
                table: "Projeto",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilizador_Email",
                table: "Utilizador",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlocoConteudo");

            migrationBuilder.DropTable(
                name: "CodigoRecuperacao");

            migrationBuilder.DropTable(
                name: "ImagemNota");

            migrationBuilder.DropTable(
                name: "ImagemProjeto");

            migrationBuilder.DropTable(
                name: "Utilizador");

            migrationBuilder.DropTable(
                name: "Nota");

            migrationBuilder.DropTable(
                name: "Projeto");
        }
    }
}
