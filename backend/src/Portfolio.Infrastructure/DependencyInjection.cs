using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Core.Autenticacao;
using Portfolio.Core.Conteudo;
using Portfolio.Core.Imagens;
using Portfolio.Core.Projetos;
using Portfolio.Infrastructure.Autenticacao;
using Portfolio.Infrastructure.Conteudo;
using Portfolio.Infrastructure.Dados;
using Portfolio.Infrastructure.Github;
using Portfolio.Infrastructure.Imagens;

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

        // Imagens (SEG17–SEG20). As opções são lidas quando são precisas, já com toda a configuração.
        services.AddSingleton(_ => configuracao.GetSection("Imagens").Get<OpcoesImagens>() ?? new OpcoesImagens());
        services.AddSingleton<IProcessadorImagens, ProcessadorSkia>();
        services.AddSingleton<IArmazenamentoImagens, ArmazenamentoDiscoLocal>();
        services.AddScoped<IRepositorioImagens, RepositorioImagens>();

        // GitHub (SEG29): só api.github.com, tempo limite de 10 s, token opcional (SEG25).
        services.AddSingleton(_ => configuracao.GetSection("Github").Get<OpcoesTimeline>() ?? new OpcoesTimeline());
        services.AddSingleton<IGithub, GithubApi>();
        services.AddHttpClient(GithubApi.Cliente, c =>
        {
            c.BaseAddress = new Uri("https://api.github.com/");
            c.Timeout = TimeSpan.FromSeconds(10);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("Portfolio-Pessoal");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            var token = configuracao["Github:Token"];
            if (!string.IsNullOrWhiteSpace(token))
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });

        return services;
    }

    internal static string ObterConnectionString(IConfiguration configuracao, string nome) =>
        configuracao.GetConnectionString(nome)
        ?? throw new InvalidOperationException(
            $"Falta a connection string '{nome}'. Ver o README (user-secrets ou variável ConnectionStrings__{nome}).");
}
