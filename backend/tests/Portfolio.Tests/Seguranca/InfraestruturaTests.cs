using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portfolio.Tests.Seguranca;

// Medidas de infraestrutura que não precisam da base de dados (docs/06-revisao-seguranca.md).
public class InfraestruturaTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // SEG22: em produção, o browser fica a saber que o site só se abre por HTTPS durante um ano.
    [Fact]
    public async Task Em_producao_as_respostas_levam_hsts()
    {
        await using var fabrica = Fabrica("Production");
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://portfolio.exemplo") });

        var resposta = await cliente.GetAsync("/api/backoffice/auth/csrf", Ct);

        Assert.Equal("max-age=31536000; includeSubDomains", resposta.Headers.GetValues("Strict-Transport-Security").Single());
    }

    // Em desenvolvimento não há HSTS, senão o browser ficava preso ao HTTPS no localhost.
    [Fact]
    public async Task Em_desenvolvimento_nao_ha_hsts()
    {
        await using var fabrica = Fabrica("Development");
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://portfolio.exemplo") });

        var resposta = await cliente.GetAsync("/api/backoffice/auth/csrf", Ct);

        Assert.False(resposta.Headers.Contains("Strict-Transport-Security"));
    }

    // SEG24: só os frontends configurados (Cors:Origens) podem fazer pedidos com credenciais.
    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("http://localhost:3000", true)]
    [InlineData("https://evil.example", false)]
    [InlineData("http://localhost:5173.evil.example", false)]
    public async Task O_cors_so_aceita_os_frontends(string origem, bool aceite)
    {
        await using var fabrica = Fabrica("Development");
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var pedido = new HttpRequestMessage(HttpMethod.Options, "/api/backoffice/auth/login");
        pedido.Headers.Add("Origin", origem);
        pedido.Headers.Add("Access-Control-Request-Method", "POST");
        pedido.Headers.Add("Access-Control-Request-Headers", "content-type,x-xsrf-token");

        var resposta = await cliente.SendAsync(pedido, Ct);

        if (aceite)
        {
            Assert.Equal(origem, resposta.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Equal("true", resposta.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        }
        else
        {
            Assert.False(resposta.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }

    // SEG26: um erro inesperado em produção responde em ProblemDetails, sem stack trace nem
    // mensagens internas. Aqui o erro é a base de dados não existir.
    [Fact]
    public async Task Em_producao_os_erros_nao_mostram_detalhes_internos()
    {
        await using var fabrica = Fabrica("Production");
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://portfolio.exemplo") });

        var resposta = await cliente.GetAsync("/api/publico/blocos", Ct);
        var corpo = await resposta.Content.ReadAsStringAsync(Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Npgsql", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", corpo);
        Assert.DoesNotContain(" at ", corpo);
    }

    // Ligações fictícias: os controllers precisam delas para serem criados, mas estes testes
    // não chegam à base de dados (exceto o dos erros, que usa uma porta onde não há nada).
    private const string SemBaseDeDados = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=2";

    private static WebApplicationFactory<Program> Fabrica(string ambiente, params (string Chave, string Valor)[] configuracao) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment(ambiente);
            b.UseSetting("ConnectionStrings:Escrita", SemBaseDeDados);
            b.UseSetting("ConnectionStrings:Leitura", SemBaseDeDados);
            foreach (var (chave, valor) in configuracao)
                b.UseSetting(chave, valor);
        });
}
