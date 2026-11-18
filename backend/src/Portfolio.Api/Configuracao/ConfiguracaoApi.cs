using Microsoft.AspNetCore.DataProtection;
using Portfolio.Api.Autenticacao;
using Portfolio.Core.Conteudo;
using Portfolio.Core.Projetos;
using Portfolio.Infrastructure;
using Scalar.AspNetCore;

namespace Portfolio.Api.Configuracao;

public static class ConfiguracaoApi
{
    public static IServiceCollection AddPortfolioApi(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddInfrastructure(configuracao);
        AddDataProtection(services, configuracao);
        services.AddAutenticacao(configuracao);
        services.AddScoped<BlocoConteudoService>();
        services.AddScoped<ProjetoService>();

        // SEG11: token CSRF obrigatório em todos os pedidos de escrita (POST, PUT, DELETE).
        services.AddControllers(o => o.Filters.Add<FiltroCsrf>());
        services.AddOpenApi();

        // SEG26: erros em ProblemDetails, sem stack traces fora de desenvolvimento.
        services.AddProblemDetails();

        // SEG24: só os domínios dos dois frontends (Cors:Origens no appsettings).
        var origens = configuracao.GetSection("Cors:Origens").Get<string[]>() ?? [];
        services.AddCors(o => o.AddDefaultPolicy(p => p
            .WithOrigins(origens)
            .AllowCredentials()
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", Sessao.CabecalhoCsrf)));

        // SEG22
        services.AddHsts(o =>
        {
            o.MaxAge = TimeSpan.FromDays(365);
            o.IncludeSubDomains = true;
        });

        return services;
    }

    public static WebApplication UsePortfolioApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        app.UseHttpsRedirection();
        app.Use(CabecalhosSeguranca(app.Environment));
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        if (app.Environment.IsDevelopment())
        {
            // Documentação da API, só em desenvolvimento:
            // /openapi/v1.json (especificação) e /scalar (página para testar os endpoints).
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference().AllowAnonymous();
        }

        app.MapControllers();
        return app;
    }

    // Chaves usadas para cifrar o cookie de sessão e o segredo do 2FA (SEG09). Em produção ficam
    // numa pasta própria (DataProtection:PastaChaves), fora da base de dados e da pasta da aplicação.
    private static void AddDataProtection(IServiceCollection services, IConfiguration configuracao)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Portfolio");
        var pasta = configuracao["DataProtection:PastaChaves"];
        if (!string.IsNullOrWhiteSpace(pasta))
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(pasta));
    }

    // SEG23. A API só devolve JSON, por isso a CSP não deixa carregar nada.
    // A página do Scalar (só em desenvolvimento) precisa de scripts e fica de fora.
    private static Func<HttpContext, RequestDelegate, Task> CabecalhosSeguranca(IHostEnvironment ambiente) =>
        (contexto, seguinte) =>
        {
            var cabecalhos = contexto.Response.Headers;
            cabecalhos.XContentTypeOptions = "nosniff";
            cabecalhos.XFrameOptions = "DENY";
            cabecalhos["Referrer-Policy"] = "no-referrer";

            var scalar = ambiente.IsDevelopment() && contexto.Request.Path.StartsWithSegments("/scalar");
            if (!scalar)
                cabecalhos.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

            return seguinte(contexto);
        };
}
