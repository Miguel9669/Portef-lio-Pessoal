using Microsoft.EntityFrameworkCore;
using Npgsql;
using Portfolio.Core.Entidades;

namespace Portfolio.Tests.BaseDeDados;

[Collection("BaseDeDados")]
public class BaseDeDadosTests(PostgresFixture bd) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        bd.Exigir();
        // Cada teste começa com as tabelas vazias.
        await using var contexto = bd.NovoBackoffice();
        await contexto.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"Projeto\"; DELETE FROM \"BlocoConteudo\"; DELETE FROM \"Utilizador\";", Ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_migracao_cria_as_5_tabelas()
    {
        await using var contexto = bd.NovoBackoffice();

        var tabelas = await contexto.Database
            .SqlQueryRaw<string>("""
                SELECT table_name AS "Value" FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
                ORDER BY table_name
                """)
            .ToListAsync(Ct);

        Assert.Equal(
            ["BlocoConteudo", "CodigoRecuperacao", "ImagemProjeto", "Projeto", "Utilizador"],
            tabelas);
    }

    [Fact]
    public async Task So_pode_existir_um_utilizador()
    {
        await using (var contexto = bd.NovoBackoffice())
        {
            contexto.Utilizadores.Add(NovoUtilizador("admin@exemplo.pt"));
            await contexto.SaveChangesAsync(Ct);
        }

        await using (var contexto = bd.NovoBackoffice())
        {
            contexto.Utilizadores.Add(NovoUtilizador("outro@exemplo.pt"));
            var erro = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync(Ct));
            Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)erro.InnerException!).SqlState);
        }

        await using (var contexto = bd.NovoBackoffice())
        {
            // Mesmo com outro Id, o CHECK (Id = 1) recusa.
            contexto.Utilizadores.Add(NovoUtilizador("outro@exemplo.pt", id: 2));
            var erro = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync(Ct));
            Assert.Equal(PostgresErrorCodes.CheckViolation, ((PostgresException)erro.InnerException!).SqlState);
        }
    }

    [Fact]
    public async Task Uma_tentativa_falhada_nao_impede_criar_a_conta_depois()
    {
        await using (var contexto = bd.NovoBackoffice())
        {
            contexto.Utilizadores.Add(NovoUtilizador(new string('a', 300) + "@exemplo.pt")); // nome demasiado longo
            contexto.Utilizadores.Local.Single().Nome = new string('x', 151);
            await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync(Ct));
        }

        await using (var contexto = bd.NovoBackoffice())
        {
            contexto.Utilizadores.Add(NovoUtilizador("admin@exemplo.pt"));
            await contexto.SaveChangesAsync(Ct);
        }
    }

    [Fact]
    public async Task O_email_nao_distingue_maiusculas()
    {
        await using (var contexto = bd.NovoBackoffice())
        {
            contexto.Utilizadores.Add(NovoUtilizador("Admin@Exemplo.pt"));
            await contexto.SaveChangesAsync(Ct);
        }

        await using var leitura = bd.NovoBackoffice();
        Assert.True(await leitura.Utilizadores.AnyAsync(u => u.Email == "admin@exemplo.PT", Ct));
    }

    [Fact]
    public async Task As_datas_sao_preenchidas_em_utc()
    {
        var antes = DateTime.UtcNow;
        await using var contexto = bd.NovoBackoffice();
        var projeto = new Projeto { Nome = "Teste", Slug = "teste" };
        contexto.Projetos.Add(projeto);
        await contexto.SaveChangesAsync(Ct);

        Assert.True(projeto.CriadoEm >= antes);
        Assert.Equal(projeto.CriadoEm, projeto.AtualizadoEm);

        await Task.Delay(10, Ct);
        projeto.Descricao = "alterado";
        await contexto.SaveChangesAsync(Ct);

        Assert.True(projeto.AtualizadoEm > projeto.CriadoEm);

        await using var outro = bd.NovoBackoffice();
        var lido = await outro.Projetos.SingleAsync(Ct);
        Assert.Equal(DateTimeKind.Utc, lido.CriadoEm.Kind);
    }

    [Fact]
    public async Task Apagar_um_projeto_apaga_as_imagens()
    {
        await using (var contexto = bd.NovoBackoffice())
        {
            contexto.Projetos.Add(new Projeto
            {
                Nome = "Com timeline",
                Slug = "com-timeline",
                RepositorioGithub = "https://github.com/dono/repositorio",
                Imagens = [NovaImagemProjeto()],
            });
            await contexto.SaveChangesAsync(Ct);
        }

        await using (var contexto = bd.NovoBackoffice())
        {
            await contexto.Projetos.ExecuteDeleteAsync(Ct);

            Assert.Equal(0, await contexto.ImagensProjeto.CountAsync(Ct));
        }
    }

    // SEG29: mesmo que a validação da aplicação falhe, só ficam gravados links do GitHub.
    [Theory]
    [InlineData("https://evil.example/dono/repositorio")]
    [InlineData("http://github.com/dono/repositorio")]
    [InlineData("https://github.com.evil.example/dono/repositorio")]
    [InlineData("https://github.com/dono/repositorio/../../outra-coisa")]
    [InlineData("https://github.com/dono")]
    public async Task A_base_de_dados_so_aceita_links_de_repositorios_do_github(string link)
    {
        await using var contexto = bd.NovoBackoffice();
        contexto.Projetos.Add(new Projeto { Nome = "Teste", Slug = "teste", RepositorioGithub = link });

        var erro = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync(Ct));
        Assert.Equal(PostgresErrorCodes.CheckViolation, Assert.IsType<PostgresException>(erro.InnerException).SqlState);
    }

    [Fact]
    public async Task Um_projeto_pode_nao_ter_repositorio_ou_ter_um_do_github()
    {
        await using var contexto = bd.NovoBackoffice();
        contexto.Projetos.Add(new Projeto { Nome = "Sem repo", Slug = "sem-repo" });
        contexto.Projetos.Add(new Projeto { Nome = "Com repo", Slug = "com-repo", RepositorioGithub = "https://github.com/Miguel9669/Portef-lio-Pessoal" });

        await contexto.SaveChangesAsync(Ct);

        Assert.Equal(2, await contexto.Projetos.CountAsync(Ct));
    }

    // SEG01: o site público não consegue ler a conta do backoffice, nem com um erro no código.
    [Fact]
    public async Task O_utilizador_de_leitura_nao_le_a_conta_do_backoffice()
    {
        await using (var escrita = bd.NovoBackoffice())
        {
            escrita.Utilizadores.Add(NovoUtilizador("admin@exemplo.pt"));
            await escrita.SaveChangesAsync(Ct);
        }

        await using var publico = bd.NovoPublico();

        var conta = await Assert.ThrowsAsync<PostgresException>(() => publico.Utilizadores.ToListAsync(Ct));
        var codigos = await Assert.ThrowsAsync<PostgresException>(() => publico.CodigosRecuperacao.ToListAsync(Ct));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, conta.SqlState);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, codigos.SqlState);
        Assert.Empty(await publico.Projetos.ToListAsync(Ct));
    }

    [Fact]
    public async Task O_contexto_publico_nao_grava()
    {
        await using var contexto = bd.NovoPublico();
        contexto.Projetos.Add(new Projeto { Nome = "Intruso", Slug = "intruso" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => contexto.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task O_utilizador_de_leitura_e_recusado_pela_base_de_dados()
    {
        await using (var escrita = bd.NovoBackoffice())
        {
            escrita.Projetos.Add(new Projeto { Nome = "Existente", Slug = "existente", Publico = true });
            await escrita.SaveChangesAsync(Ct);
        }

        await using var contexto = bd.NovoPublico();

        // Ler funciona.
        Assert.Equal(1, await contexto.Projetos.CountAsync(Ct));

        // ExecuteDelete/Update não passam pelo SaveChanges: é a base de dados que recusa.
        var apagar = await Assert.ThrowsAsync<PostgresException>(() => contexto.Projetos.ExecuteDeleteAsync(Ct));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, apagar.SqlState);

        var alterar = await Assert.ThrowsAsync<PostgresException>(() =>
            contexto.Projetos.ExecuteUpdateAsync(s => s.SetProperty(p => p.Publico, false), Ct));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, alterar.SqlState);
    }

    private static Utilizador NovoUtilizador(string email, int id = 1) => new()
    {
        Id = id,
        Nome = "Admin",
        Email = email,
        PasswordHash = "$argon2id$v=19$m=19456,t=2,p=1$c2FsdA$aGFzaA",
    };

    private static ImagemProjeto NovaImagemProjeto() => new()
    {
        Ficheiro = $"{Guid.NewGuid():N}.webp",
        NomeOriginal = "foto.jpg",
        TipoMime = "image/webp",
        TamanhoBytes = 1234,
    };
}
