using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Portfolio.Core.Entidades;

namespace Portfolio.Api.Autenticacao;

// Valores da secção "Autenticacao" do appsettings.json que dizem respeito à sessão e ao rate limit.
public sealed class OpcoesSessao
{
    // SEG12
    public TimeSpan Inatividade { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan DuracaoMaxima { get; set; } = TimeSpan.FromHours(8);

    // Sessão parcial, entre a password e o código do 2FA.
    public TimeSpan DuracaoSessaoParcial { get; set; } = TimeSpan.FromMinutes(5);
    public int TentativasCodigoPorSessao { get; set; } = 5;

    // SEG06: tentativas por IP em cada janela.
    public int LoginsPorIp { get; set; } = 5;
    public int CodigosPorIp { get; set; } = 10;
    public TimeSpan JanelaPorIp { get; set; } = TimeSpan.FromMinutes(15);
}

public static class Sessao
{
    public const string Esquema = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string Cookie = "portfolio.sessao";

    // "parcial": só a password, à espera do 2FA. "completa": password e 2FA.
    public const string ClaimEtapa = "etapa";
    public const string Parcial = "parcial";
    public const string Completa = "completa";

    public const string ClaimStamp = "stamp";
    public const string ClaimInicio = "inicio";
    public const string ClaimId = "sessao";

    public const string PoliticaParcial = "SessaoParcial";
    public const string LimiteLogin = "login";
    public const string LimiteCodigo = "codigo";

    // CSRF (SEG11): o frontend lê o cookie e devolve o valor no cabeçalho em cada pedido de escrita.
    public const string CookieCsrf = "XSRF-TOKEN";
    public const string CabecalhoCsrf = "X-XSRF-TOKEN";

    public static DateTimeOffset Inicio(ClaimsPrincipal utilizador) =>
        DateTimeOffset.FromUnixTimeSeconds(long.Parse(utilizador.FindFirstValue(ClaimInicio) ?? "0", CultureInfo.InvariantCulture));
}

// Abre e fecha as sessões e conta as tentativas do código do 2FA.
public sealed class GestorSessao(
    IHttpContextAccessor acessor,
    IAntiforgery antiforgery,
    IMemoryCache cache,
    TimeProvider relogio,
    IOptions<OpcoesSessao> opcoes)
{
    private HttpContext Http => acessor.HttpContext ?? throw new InvalidOperationException("Sem pedido HTTP.");

    public Task AbrirParcialAsync(Utilizador utilizador) => AbrirAsync(utilizador, Sessao.Parcial);

    public Task AbrirCompletaAsync(Utilizador utilizador) => AbrirAsync(utilizador, Sessao.Completa);

    // Abrir uma sessão nova apaga a anterior do servidor (a parcial, quando se completa o 2FA).
    private async Task AbrirAsync(Utilizador utilizador, string etapa)
    {
        var agora = relogio.GetUtcNow();
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, utilizador.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, utilizador.Email),
            new(Sessao.ClaimEtapa, etapa),
            new(Sessao.ClaimStamp, utilizador.SecurityStamp),
            new(Sessao.ClaimInicio, agora.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
            new(Sessao.ClaimId, Guid.NewGuid().ToString("N")),
        ];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Sessao.Esquema));

        var parcial = etapa == Sessao.Parcial;
        await Http.SignInAsync(Sessao.Esquema, principal, new AuthenticationProperties
        {
            IssuedUtc = agora,
            ExpiresUtc = agora + (parcial ? opcoes.Value.DuracaoSessaoParcial : opcoes.Value.Inatividade),
            // A sessão parcial não se prolonga com a atividade.
            AllowRefresh = !parcial,
        });
        EmitirTokenCsrf(principal);
    }

    // Termina a sessão também no servidor (SEG12): o cookie antigo deixa de servir.
    public async Task FecharAsync()
    {
        await Http.SignOutAsync(Sessao.Esquema);
        EmitirTokenCsrf(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    // Devolve true quando se esgotam as tentativas; nesse caso fecha a sessão parcial e
    // é preciso recomeçar pela password (SEG06, SEG09).
    public async Task<bool> RegistarCodigoErradoAsync()
    {
        var chave = "2fa:" + Http.User.FindFirstValue(Sessao.ClaimId);
        var falhas = cache.Get<int>(chave) + 1;
        if (falhas < opcoes.Value.TentativasCodigoPorSessao)
        {
            cache.Set(chave, falhas, opcoes.Value.DuracaoSessaoParcial);
            return false;
        }

        cache.Remove(chave);
        await FecharAsync();
        return true;
    }

    // O token CSRF está ligado ao utilizador, por isso emite-se outro sempre que a sessão muda.
    public void EmitirTokenCsrf(ClaimsPrincipal utilizador)
    {
        var anterior = Http.User;
        Http.User = utilizador;
        var tokens = antiforgery.GetAndStoreTokens(Http);
        Http.User = anterior;

        Http.Response.Cookies.Append(Sessao.CookieCsrf, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
    }
}
