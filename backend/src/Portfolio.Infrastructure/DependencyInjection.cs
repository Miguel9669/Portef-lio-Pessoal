using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Core.Autenticacao;
using Portfolio.Core.Conteudo;
using Portfolio.Core.Projetos;
using Portfolio.Infrastructure.Autenticacao;
using Portfolio.Infrastructure.Conteudo;
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

        // Autenticação. A cifra precisa da Data Protection API, configurada na API.
        services.AddScoped<IRepositorioUtilizador, RepositorioUtilizador>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<ITotp, TotpOtpNet>();
        services.AddSingleton<ICifraSegredos, CifraDataProtection>();

        // Conteúdo e projetos.
        services.AddScoped<IRepositorioBlocos, RepositorioBlocos>();
        services.AddScoped<IRepositorioProjetos, RepositorioProjetos>();
        services.AddSingleton<IConversorMarkdown, ConversorMarkdig>();

        return services;
    }

    internal static string ObterConnectionString(IConfiguration configuracao, string nome) =>
        configuracao.GetConnectionString(nome)
        ?? throw new InvalidOperationException(
            $"Falta a connection string '{nome}'. Ver o README (user-secrets ou variável ConnectionStrings__{nome}).");
}
