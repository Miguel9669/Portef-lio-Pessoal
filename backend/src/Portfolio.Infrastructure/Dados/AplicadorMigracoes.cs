using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Portfolio.Infrastructure.Dados;

public static class AplicadorMigracoes
{
    // Aplica as migrações com o utilizador portfolio_migracoes, o único com permissões
    // para criar e alterar tabelas (SEG01). Usado pelo comando "migrar" da API.
    public static async Task AplicarAsync(IConfiguration configuracao, CancellationToken ct = default)
    {
        var opcoes = new DbContextOptionsBuilder<BackofficeDbContext>()
            .UseNpgsql(DependencyInjection.ObterConnectionString(configuracao, "Migracoes"))
            .Options;

        await using var contexto = new BackofficeDbContext(opcoes);
        await contexto.Database.MigrateAsync(ct);
    }
}
