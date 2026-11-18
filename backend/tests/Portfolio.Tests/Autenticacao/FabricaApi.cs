using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OtpNet;
using Portfolio.Core.Autenticacao;
using Portfolio.Tests.BaseDeDados;

namespace Portfolio.Tests.Autenticacao;

// A API completa, ligada ao PostgreSQL do Testcontainers, com um relógio que os testes controlam.
public sealed class FabricaApi(
    PostgresFixture bd,
    Dictionary<string, string?>? configuracao = null,
    Action<IServiceCollection>? servicos = null)
    : WebApplicationFactory<Program>
{
    public const string Email = "admin@exemplo.pt";
    public const string Password = "uma-password-comprida";

    public FakeTimeProvider Relogio { get; } = new(DateTimeOffset.UtcNow);

    // Cada fábrica guarda as imagens numa pasta temporária própria, apagada no fim.
    public string PastaImagens { get; } = Path.Combine(Path.GetTempPath(), "portfolio-testes", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, c) =>
        {
            c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Escrita"] = bd.ConnEscrita,
                ["ConnectionStrings:Leitura"] = bd.ConnLeitura,
                // Todos os pedidos dos testes vêm do mesmo "IP"; o rate limit tem testes próprios.
                ["Autenticacao:LoginsPorIp"] = "1000",
                ["Autenticacao:CodigosPorIp"] = "1000",
                ["SitePublico:PedidosPorMinutoPorIp"] = "1000",
                ["Imagens:Pasta"] = PastaImagens,
            });
            if (configuracao is not null)
                c.AddInMemoryCollection(configuracao);
        });
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<TimeProvider>();
            s.AddSingleton<TimeProvider>(Relogio);
            s.AddDataProtection().UseEphemeralDataProtectionProvider();
            servicos?.Invoke(s);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(PastaImagens))
            Directory.Delete(PastaImagens, recursive: true);
    }

    public ClienteTeste NovoCliente() => new(CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
        AllowAutoRedirect = false,
    }));

    // Cliente com o token CSRF, como o frontend ao arrancar.
    public async Task<ClienteTeste> NovoClienteComCsrfAsync()
    {
        var cliente = NovoCliente();
        (await cliente.GetAsync("/api/backoffice/auth/csrf")).EnsureSuccessStatusCode();
        return cliente;
    }

    public async Task CriarAdminAsync()
    {
        await using var ambito = Services.CreateAsyncScope();
        var resultado = await ambito.ServiceProvider.GetRequiredService<ContaService>()
            .CriarAdminAsync("Admin", Email, Password, TestContext.Current.CancellationToken);
        Assert.Equal(ResultadoConta.Sucesso, resultado);
    }

    public Task<HttpResponseMessage> LoginAsync(ClienteTeste cliente, string email = Email, string password = Password) =>
        cliente.PostAsync("/api/backoffice/auth/login", new { email, password });

    // Login, configuração e ativação do 2FA. Devolve o segredo e os códigos de recuperação.
    public async Task<(string Segredo, List<string> CodigosRecuperacao)> EntrarComDoisFatoresAsync(ClienteTeste cliente)
    {
        (await LoginAsync(cliente)).EnsureSuccessStatusCode();
        var configuracao = await cliente.PostAsync("/api/backoffice/auth/2fa/configurar");
        var segredo = (await configuracao.Content.ReadFromJsonAsync<ConfiguracaoTotp>(TestContext.Current.CancellationToken))!.Segredo;

        var ativacao = await cliente.PostAsync("/api/backoffice/auth/2fa/ativar", new { codigo = Codigo(segredo) });
        ativacao.EnsureSuccessStatusCode();
        var corpo = await ativacao.Content.ReadFromJsonAsync<RespostaCodigos>(TestContext.Current.CancellationToken);
        return (segredo, corpo!.CodigosRecuperacao);
    }

    public string Codigo(string segredo) =>
        new Totp(Base32Encoding.ToBytes(segredo)).ComputeTotp(Relogio.GetUtcNow().UtcDateTime);

    // Avança para o intervalo de 30 s seguinte, para gerar um código novo.
    public void AvancarParaProximoCodigo() => Relogio.Advance(TimeSpan.FromSeconds(30));

    public sealed record RespostaCodigos(List<string> CodigosRecuperacao);
}

// HttpClient com cookies geridos à mão, para poder copiar uma sessão para outro cliente.
// Envia o token CSRF no cabeçalho, como o frontend.
public sealed class ClienteTeste(HttpClient http)
{
    public Dictionary<string, string> Cookies { get; } = [];

    public Task<HttpResponseMessage> GetAsync(string url) => EnviarAsync(HttpMethod.Get, url);

    public Task<HttpResponseMessage> PostAsync(string url, object? corpo = null) => EnviarAsync(HttpMethod.Post, url, corpo);

    public async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, object? corpo = null, bool comCsrf = true)
    {
        using var pedido = new HttpRequestMessage(metodo, url);
        if (corpo is not null)
            pedido.Content = corpo as HttpContent ?? JsonContent.Create(corpo);
        if (Cookies.Count > 0)
            pedido.Headers.Add("Cookie", string.Join("; ", Cookies.Select(c => $"{c.Key}={c.Value}")));
        if (comCsrf && Cookies.TryGetValue("XSRF-TOKEN", out var csrf))
            pedido.Headers.Add("X-XSRF-TOKEN", csrf);

        var resposta = await http.SendAsync(pedido, TestContext.Current.CancellationToken);
        GuardarCookies(resposta);
        return resposta;
    }

    public ClienteTeste CopiarSessaoPara(ClienteTeste outro)
    {
        foreach (var (nome, valor) in Cookies)
            outro.Cookies[nome] = valor;
        return outro;
    }

    private void GuardarCookies(HttpResponseMessage resposta)
    {
        if (!resposta.Headers.TryGetValues("Set-Cookie", out var valores))
            return;

        foreach (var cookie in valores)
        {
            var par = cookie.Split(';')[0];
            var igual = par.IndexOf('=');
            var nome = par[..igual];
            var valor = par[(igual + 1)..];
            var apagado = valor.Length == 0 || cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);
            if (apagado)
                Cookies.Remove(nome);
            else
                Cookies[nome] = valor;
        }
    }
}
