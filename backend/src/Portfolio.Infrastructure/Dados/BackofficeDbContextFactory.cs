using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Portfolio.Infrastructure.Dados;

// Usada só pelo "dotnet ef migrations add", que não precisa de ligar à base de dados.
// Para aplicar migrações usa-se o comando "migrar" da API.
public class BackofficeDbContextFactory : IDesignTimeDbContextFactory<BackofficeDbContext>
{
    public BackofficeDbContext CreateDbContext(string[] args)
    {
        var opcoes = new DbContextOptionsBuilder<BackofficeDbContext>()
            .UseNpgsql("Host=localhost;Database=portfolio;Username=portfolio_migracoes")
            .Options;
        return new BackofficeDbContext(opcoes);
    }
}
