using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class LeituraSemConta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SEG01: o utilizador do site público (só leitura) não precisa da conta do backoffice.
            // Sem isto, um erro no lado público podia expor o hash da password, o segredo cifrado
            // do 2FA e os códigos de recuperação. As tabelas novas continuam a receber SELECT por
            // defeito (docker/postgres/init); uma tabela sensível nova tem de ser excluída aqui.
            migrationBuilder.Sql("REVOKE ALL ON \"Utilizador\", \"CodigoRecuperacao\" FROM portfolio_leitura;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("GRANT SELECT ON \"Utilizador\", \"CodigoRecuperacao\" TO portfolio_leitura;");
        }
    }
}
