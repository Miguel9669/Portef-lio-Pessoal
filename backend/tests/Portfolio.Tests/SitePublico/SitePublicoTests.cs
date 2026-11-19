using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portfolio.Api.Projetos;
using Portfolio.Core.Projetos;
using Portfolio.Core.SitePublico;
using Portfolio.Tests.Autenticacao;
using Portfolio.Tests.BaseDeDados;
using Portfolio.Tests.Imagens;
using Portfolio.Tests.Projetos;
using SkiaSharp;

namespace Portfolio.Tests.SitePublico;

// API pública (fase 5). O conteúdo é criado pelo backoffice, como na realidade,
// e lido por um cliente sem sessão.
[Collection("BaseDeDados")]
public sealed class SitePublicoTests(PostgresFixture bd) : IAsyncLifetime
{
    private const string Publico = "/api/publico";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly GithubFalso _github = new();
    private FabricaApi _api = null!;
    private ClienteTeste _backoffice = null!;
    private ClienteTeste _visitante = null!;
    private RespostaProjeto _publico = null!;
    private RespostaProjeto _privado = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        bd.Exigir();
        await using (var contexto = bd.NovoBackoffice())
        {
            await contexto.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Projeto\"; DELETE FROM \"BlocoConteudo\"; DELETE FROM \"Utilizador\";", Ct);
        }

        _api = new FabricaApi(bd, servicos: s =>
        {
            s.RemoveAll<IGithub>();
            s.AddSingleton<IGithub>(_github);
        });
        await _api.CriarAdminAsync();
        _backoffice = await _api.NovoClienteComCsrfAsync();
        await _api.EntrarComDoisFatoresAsync(_backoffice);
        _visitante = _api.NovoCliente();

        _publico = await CriarProjeto("Projeto Público", "https://github.com/dono/publico", publicar: true);
        _privado = await CriarProjeto("Projeto Secreto", "https://github.com/dono/secreto", publicar: false);
    }

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task Os_blocos_publicos_vem_por_ordem_com_o_html_sanitizado()
    {
        await CriarBloco("Entrada", "# Olá\n\n<script>alert(1)</script>", publico: true);
        await CriarBloco("Rascunho", "Ainda não", publico: false);
        await CriarBloco("Contactos", "[email](mailto:eu@exemplo.pt)", publico: true);

        var blocos = await Ler<List<BlocoPublico>>($"{Publico}/blocos");

        Assert.Equal(["Entrada", "Contactos"], blocos.Select(b => b.Titulo));
        Assert.Contains("<h1", blocos[0].Html);
        Assert.DoesNotContain("<script", blocos[0].Html);
        Assert.Contains("href=\"mailto:eu@exemplo.pt\"", blocos[1].Html);
    }

    [Fact]
    public async Task Lista_e_mostra_os_projetos_publicos_sem_login()
    {
        var lista = await Ler<List<ProjetoPublico>>($"{Publico}/projetos");
        var projeto = await Ler<ProjetoPublico>($"{Publico}/projetos/{_publico.Slug}");
        var timeline = await Ler<RespostaTimeline>($"{Publico}/projetos/{_publico.Slug}/timeline");
        var imagem = await _visitante.GetAsync(projeto.Imagens[0].Url);

        Assert.Equal([_publico.Slug], lista.Select(p => p.Slug));
        Assert.Equal("https://github.com/dono/publico", projeto.RepositorioGithub);
        Assert.StartsWith("/api/publico/imagens/", projeto.Imagens[0].Url);
        Assert.Equal(2, timeline.Commits.Count);
        Assert.Equal(HttpStatusCode.OK, imagem.StatusCode);
        Assert.Equal("image/png", imagem.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", imagem.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("immutable", imagem.Headers.CacheControl?.ToString());
    }

    // SEG16: o teste que fecha a fase. Nada de um projeto não público sai por nenhum endpoint.
    [Fact]
    public async Task Nada_de_um_projeto_nao_publico_aparece_em_nenhum_endpoint_publico()
    {
        // O nome real do ficheiro da imagem privada, como se alguém o tivesse adivinhado.
        string ficheiroPrivado;
        await using (var contexto = bd.NovoBackoffice())
            ficheiroPrivado = (await contexto.ImagensProjeto.SingleAsync(i => i.ProjetoId == _privado.Id, Ct)).Ficheiro;
        Assert.True(File.Exists(Path.Combine(_api.PastaImagens, ficheiroPrivado)));
        var pedidosAoGithub = _github.Pedidos;

        var lista = await _visitante.GetAsync($"{Publico}/projetos");
        var projeto = await _visitante.GetAsync($"{Publico}/projetos/{_privado.Slug}");
        var timeline = await _visitante.GetAsync($"{Publico}/projetos/{_privado.Slug}/timeline");
        var imagem = await _visitante.GetAsync($"{Publico}/imagens/{ficheiroPrivado}");
        var conteudoLista = await lista.Content.ReadAsStringAsync(Ct);

        Assert.DoesNotContain("Secreto", conteudoLista);
        Assert.DoesNotContain(_privado.Slug, conteudoLista);
        Assert.Equal(HttpStatusCode.NotFound, projeto.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, timeline.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, imagem.StatusCode);
        // Nem chega a pedir ao GitHub os commits do repositório privado.
        Assert.Equal(pedidosAoGithub, _github.Pedidos);
    }

    // SEG29: com o código privado, o site não recebe o link do repositório e nem se pede ao GitHub.
    [Fact]
    public async Task Um_projeto_com_codigo_privado_nao_mostra_o_repositorio_nem_o_historico()
    {
        var id = await CriarPublicado(new { nome = "Privado", repositorioGithub = "https://github.com/dono/privado", codigoPrivado = true });
        var pedidosAoGithub = _github.Pedidos;

        var projeto = await Ler<ProjetoPublico>($"{Publico}/projetos/privado");
        var timeline = await Ler<RespostaTimeline>($"{Publico}/projetos/privado/timeline");
        var lista = await _visitante.GetAsync($"{Publico}/projetos");
        var noBackoffice = await Ler<RespostaTimeline>($"/api/backoffice/projetos/{id}/timeline", _backoffice);

        Assert.Null(projeto.RepositorioGithub);
        Assert.False(projeto.TemHistorico);
        Assert.Equal(EstadoTimeline.SemRepositorio, timeline.Estado);
        Assert.DoesNotContain("github.com/dono/privado", await lista.Content.ReadAsStringAsync(Ct));
        // No backoffice os commits continuam visíveis, para uso do dono.
        Assert.Equal(EstadoTimeline.Atual, noBackoffice.Estado);
        Assert.Equal(pedidosAoGithub + 1, _github.Pedidos);
    }

    [Fact]
    public async Task Com_o_historico_publico_ligado_mostra_os_commits_mas_nao_o_link_do_codigo()
    {
        await CriarPublicado(new { nome = "Privado com historico", repositorioGithub = "https://github.com/dono/privado", codigoPrivado = true, historicoPublico = true });

        var projeto = await Ler<ProjetoPublico>($"{Publico}/projetos/privado-com-historico");
        var timeline = await Ler<RespostaTimeline>($"{Publico}/projetos/privado-com-historico/timeline");

        Assert.Null(projeto.RepositorioGithub);
        Assert.True(projeto.TemHistorico);
        Assert.Equal(2, timeline.Commits.Count);
    }

    // SEG30
    [Fact]
    public async Task O_link_oficial_aparece_no_site()
    {
        await CriarPublicado(new { nome = "Com site", linkOficial = "https://meuapp.pt" });

        var projeto = await Ler<ProjetoPublico>($"{Publico}/projetos/com-site");

        Assert.Equal("https://meuapp.pt/", projeto.LinkOficial);
        Assert.Null(projeto.RepositorioGithub);
        Assert.False(projeto.TemHistorico);
    }

    [Fact]
    public async Task Despublicar_tira_logo_o_projeto_e_as_imagens_do_site()
    {
        var urlImagem = (await Ler<ProjetoPublico>($"{Publico}/projetos/{_publico.Slug}")).Imagens[0].Url;
        var antes = await _visitante.GetAsync(urlImagem);

        await _backoffice.EnviarAsync(HttpMethod.Put, $"/api/backoffice/projetos/{_publico.Id}/publico", new { publico = false });
        var projeto = await _visitante.GetAsync($"{Publico}/projetos/{_publico.Slug}");
        var imagem = await _visitante.GetAsync(urlImagem);

        Assert.Equal(HttpStatusCode.OK, antes.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, projeto.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, imagem.StatusCode);
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("..%2F..%2Fappsettings.json")]
    [InlineData("nao-existe.png")]
    public async Task Pedidos_de_ficheiros_que_nao_sao_imagens_publicas_nao_servem_nada(string ficheiro)
    {
        var resposta = await _visitante.GetAsync($"{Publico}/imagens/{ficheiro}");

        // "../../etc/passwd" é normalizado para /api/etc/passwd, que não existe e fica
        // fechado por defeito (401); os outros chegam ao endpoint e não estão na base de dados (404).
        Assert.Contains(resposta.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Unauthorized });
        Assert.NotEqual("image", resposta.Content.Headers.ContentType?.MediaType?.Split('/')[0]);
    }

    [Fact]
    public async Task Os_endpoints_publicos_so_aceitam_leitura()
    {
        var resposta = await _visitante.EnviarAsync(HttpMethod.Delete, $"{Publico}/projetos/{_publico.Slug}");

        Assert.NotEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotEqual(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    [Fact]
    public async Task O_rate_limit_trava_muitos_pedidos_do_mesmo_ip()
    {
        await using var api = new FabricaApi(bd, new() { ["SitePublico:PedidosPorMinutoPorIp"] = "3" });
        var visitante = api.NovoCliente();

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await visitante.GetAsync($"{Publico}/projetos")).StatusCode);
        var quarto = await visitante.GetAsync($"{Publico}/projetos");

        Assert.Equal(HttpStatusCode.TooManyRequests, quarto.StatusCode);
    }

    private async Task<RespostaProjeto> CriarProjeto(string nome, string repositorio, bool publicar)
    {
        var criado = await (await _backoffice.PostAsync("/api/backoffice/projetos", new { nome, repositorioGithub = repositorio }))
            .Content.ReadFromJsonAsync<RespostaProjeto>(Ct);

        var ficheiro = new ByteArrayContent(ImagensDeTeste.Criar(SKEncodedImageFormat.Png));
        ficheiro.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var envio = await _backoffice.EnviarAsync(HttpMethod.Post, $"/api/backoffice/projetos/{criado!.Id}/imagens",
            new MultipartFormDataContent { { ficheiro, "ficheiro", "capa.png" } });
        Assert.Equal(HttpStatusCode.Created, envio.StatusCode);

        await _backoffice.EnviarAsync(HttpMethod.Put, $"/api/backoffice/projetos/{criado.Id}/publico", new { publico = publicar });
        return (await (await _backoffice.GetAsync($"/api/backoffice/projetos/{criado.Id}")).Content.ReadFromJsonAsync<RespostaProjeto>(Ct))!;
    }

    private async Task<int> CriarPublicado(object pedido)
    {
        var criado = await (await _backoffice.PostAsync("/api/backoffice/projetos", pedido)).Content.ReadFromJsonAsync<RespostaProjeto>(Ct);
        await _backoffice.EnviarAsync(HttpMethod.Put, $"/api/backoffice/projetos/{criado!.Id}/publico", new { publico = true });
        return criado.Id;
    }

    private Task<HttpResponseMessage> CriarBloco(string titulo, string conteudo, bool publico) =>
        _backoffice.PostAsync("/api/backoffice/blocos", new { titulo, conteudo, publico });

    private async Task<T> Ler<T>(string url, ClienteTeste? cliente = null)
    {
        var resposta = await (cliente ?? _visitante).GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<T>(Json, Ct))!;
    }
}
