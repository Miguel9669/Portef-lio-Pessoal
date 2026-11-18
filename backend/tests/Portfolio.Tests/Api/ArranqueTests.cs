using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portfolio.Tests.Api;

// Confirma que a API arranca e que a documentação só existe em desenvolvimento.
public class ArranqueTests
{
    [Fact]
    public async Task Em_desenvolvimento_a_documentacao_da_api_esta_disponivel()
    {
        await using var fabrica = CriarFabrica("Development");
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Em_producao_a_documentacao_da_api_nao_existe()
    {
        await using var fabrica = CriarFabrica("Production");
        using var cliente = fabrica.CreateClient();

        var resposta = await cliente.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private static WebApplicationFactory<Program> CriarFabrica(string ambiente) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment(ambiente));
}
