using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Portfolio.Core.Autenticacao;

namespace Portfolio.Api.Autenticacao;

public static class ConfiguracaoAutenticacao
{
    public static IServiceCollection AddAutenticacao(this IServiceCollection services, IConfiguration configuracao)
    {
        var seccao = configuracao.GetSection("Autenticacao");
        services.Configure<OpcoesSessao>(seccao);
        services.AddSingleton(_ => seccao.Get<OpcoesAutenticacao>() ?? new OpcoesAutenticacao());

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AutenticacaoService>();
        services.AddScoped<DoisFatoresService>();
        services.AddScoped<ContaService>();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<GestorSessao>();
        services.AddSingleton<ITicketStore, SessoesEmMemoria>();

        AddCookie(services);
        AddPoliticas(services);
        AddCsrf(services);
        AddRateLimit(services);
        return services;
    }

    // SEG10, SEG12
    private static void AddCookie(IServiceCollection services)
    {
        services.AddAuthentication(Sessao.Esquema).AddCookie(Sessao.Esquema);
        services.AddOptions<CookieAuthenticationOptions>(Sessao.Esquema)
            .Configure<IOptions<OpcoesSessao>, ITicketStore, TimeProvider>((o, sessao, store, relogio) =>
            {
                o.Cookie.Name = Sessao.Cookie;
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.ExpireTimeSpan = sessao.Value.Inatividade;
                o.SlidingExpiration = true;
                o.SessionStore = store;
                o.TimeProvider = relogio;

                // É uma API: responde 401/403 em vez de redirecionar para uma página de login.
                o.Events.OnRedirectToLogin = c => Responder(c.Response, StatusCodes.Status401Unauthorized);
                o.Events.OnRedirectToAccessDenied = c => Responder(c.Response, StatusCodes.Status403Forbidden);
                o.Events.OnValidatePrincipal = ValidadorSessao.ValidarAsync;
            });
    }

    // SEG13: fechado por defeito. Tudo exige a sessão completa (password e 2FA), exceto o que
    // tiver [AllowAnonymous] ou a política da sessão parcial.
    private static void AddPoliticas(IServiceCollection services)
    {
        var completa = new AuthorizationPolicyBuilder(Sessao.Esquema)
            .RequireAuthenticatedUser()
            .RequireClaim(Sessao.ClaimEtapa, Sessao.Completa)
            .Build();

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(completa)
            .SetFallbackPolicy(completa)
            .AddPolicy(Sessao.PoliticaParcial, p => p
                .AddAuthenticationSchemes(Sessao.Esquema)
                .RequireAuthenticatedUser()
                .RequireClaim(Sessao.ClaimEtapa, Sessao.Parcial));
    }

    // SEG11: validado em todos os pedidos de escrita (filtro global nos controllers).
    private static void AddCsrf(IServiceCollection services) =>
        services.AddAntiforgery(o =>
        {
            o.HeaderName = Sessao.CabecalhoCsrf;
            o.Cookie.Name = "portfolio.csrf";
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Strict;
        });

    // SEG06: por IP. O bloqueio por conta está no AutenticacaoService.
    private static void AddRateLimit(IServiceCollection services) =>
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Sessao.LimiteLogin, c => PorIp(c, s => s.LoginsPorIp));
            o.AddPolicy(Sessao.LimiteCodigo, c => PorIp(c, s => s.CodigosPorIp));
        });

    private static RateLimitPartition<string> PorIp(HttpContext contexto, Func<OpcoesSessao, int> limite)
    {
        var opcoes = contexto.RequestServices.GetRequiredService<IOptions<OpcoesSessao>>().Value;
        var ip = contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limite(opcoes),
            Window = opcoes.JanelaPorIp,
            QueueLimit = 0,
        });
    }

    private static Task Responder(HttpResponse resposta, int estado)
    {
        resposta.StatusCode = estado;
        return Task.CompletedTask;
    }
}
