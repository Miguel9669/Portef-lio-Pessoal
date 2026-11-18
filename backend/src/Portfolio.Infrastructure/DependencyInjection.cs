using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Infrastructure.Dados;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    // Regista os dois DbContexts, cada um com o seu utilizador da base de dados (SEG01, SEG02).
    // As connection strings vêm de user-secrets ou de variáveis de ambiente (SEG25).
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddDbContext<BackofficeDbContext>(o =>
            o.UseNpgsql(ObterConnectionString(configuracao, "Escrita")));

        services.AddDbContext<PublicoDbContext>(o =>
            o.UseNpgsql(ObterConnectionString(configuracao, "Leitura")));

        return services;
    }

    internal static string ObterConnectionString(IConfiguration configuracao, string nome) =>
        configuracao.GetConnectionString(nome)
        ?? throw new InvalidOperationException(
            $"Falta a connection string '{nome}'. Ver o README (user-secrets ou variável ConnectionStrings__{nome}).");
}
