using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Portfolio.Infrastructure.Dados;
using Testcontainers.PostgreSql;

namespace Portfolio.Tests.BaseDeDados;

// Um PostgreSQL real num contentor, partilhado pelos testes da coleção "BaseDeDados".
// Usa o mesmo script de arranque do docker-compose (docker/postgres/init), por isso
// os três utilizadores e as suas permissões são os mesmos do desenvolvimento (SEG01).
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string Db = "portfolio";
    private const string PwLeitura = "teste-leitura";
    private const string PwEscrita = "teste-escrita";
    private const string PwMigracoes = "teste-migracoes";

    private PostgreSqlContainer? _contentor;

    // Sem Docker, os testes são ignorados localmente; no CI falham (ver Exigir()).
    public string? MotivoIndisponivel { get; private set; }

    public string ConnLeitura { get; private set; } = "";
    public string ConnEscrita { get; private set; } = "";
    public string ConnMigracoes { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        try
        {
            _contentor = new PostgreSqlBuilder("postgres:18-alpine")
                .WithEnvironment("PORTFOLIO_DB", Db)
                .WithEnvironment("PORTFOLIO_LEITURA_PASSWORD", PwLeitura)
                .WithEnvironment("PORTFOLIO_ESCRITA_PASSWORD", PwEscrita)
                .WithEnvironment("PORTFOLIO_MIGRACOES_PASSWORD", PwMigracoes)
                .WithBindMount(PastaInit(), "/docker-entrypoint-initdb.d", DotNet.Testcontainers.Configurations.AccessMode.ReadOnly)
                .Build();

            await _contentor.StartAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            MotivoIndisponivel = $"Docker indisponível: {ex.Message}";
            return;
        }

        var host = $"Host={_contentor.Hostname};Port={_contentor.GetMappedPublicPort(5432)};Database={Db}";
        ConnLeitura = $"{host};Username=portfolio_leitura;Password={PwLeitura}";
        ConnEscrita = $"{host};Username=portfolio_escrita;Password={PwEscrita}";
        ConnMigracoes = $"{host};Username=portfolio_migracoes;Password={PwMigracoes}";

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Migracoes"] = ConnMigracoes })
            .Build();
        await AplicadorMigracoes.AplicarAsync(configuracao);
    }

    public async ValueTask DisposeAsync()
    {
        if (_contentor is not null)
            await _contentor.DisposeAsync();
    }

    // Chamado no início de cada teste: sem Docker, ignora localmente e falha no CI.
    public void Exigir()
    {
        if (MotivoIndisponivel is null)
            return;
        if (Environment.GetEnvironmentVariable("CI") == "true")
            Assert.Fail(MotivoIndisponivel);
        Assert.Skip(MotivoIndisponivel);
    }

    public BackofficeDbContext NovoBackoffice() =>
        new(new DbContextOptionsBuilder<BackofficeDbContext>().UseNpgsql(ConnEscrita).Options);

    public PublicoDbContext NovoPublico() =>
        new(new DbContextOptionsBuilder<PublicoDbContext>().UseNpgsql(ConnLeitura).Options);

    // Procura a pasta docker/postgres/init a partir da pasta dos testes.
    private static string PastaInit()
    {
        for (var pasta = new DirectoryInfo(AppContext.BaseDirectory); pasta is not null; pasta = pasta.Parent)
        {
            var init = Path.Combine(pasta.FullName, "docker", "postgres", "init");
            if (Directory.Exists(init))
                return init;
        }
        throw new DirectoryNotFoundException("Não encontrei docker/postgres/init.");
    }
}

[CollectionDefinition("BaseDeDados")]
public class ColecaoBaseDeDados : ICollectionFixture<PostgresFixture>;
