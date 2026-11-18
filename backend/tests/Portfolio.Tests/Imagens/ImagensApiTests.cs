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
using Portfolio.Tests.Autenticacao;
using Portfolio.Tests.BaseDeDados;
using Portfolio.Tests.Projetos;
using SkiaSharp;

namespace Portfolio.Tests.Imagens;

// Imagens e timeline pela API completa, com login e 2FA (fase 4).
[Collection("BaseDeDados")]
public sealed class ImagensApiTests(PostgresFixture bd) : IAsyncLifetime
{
    private const string Projetos = "/api/backoffice/projetos";

    private readonly GithubFalso _github = new();
    private FabricaApi _api = null!;
    private ClienteTeste _cliente = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        bd.Exigir();
        await using (var contexto = bd.NovoBackoffice())
            await contexto.Database.ExecuteSqlRawAsync("DELETE FROM \"Projeto\"; DELETE FROM \"Utilizador\";", Ct);

        _api = new FabricaApi(bd, servicos: s =>
        {
            s.RemoveAll<IGithub>();
            s.AddSingleton<IGithub>(_github);
        });
        await _api.CriarAdminAsync();
        _cliente = await _api.NovoClienteComCsrfAsync();
        await _api.EntrarComDoisFatoresAsync(_cliente);
    }

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task Envia_uma_imagem_e_ve_a_no_projeto()
    {
        var projeto = await CriarProjeto();

        var envio = await Enviar(projeto.Id, ImagensDeTeste.JpegComExif(), "../../férias.JPG");
        var imagem = (await envio.Content.ReadFromJsonAsync<RespostaImagem>(Ct))!;
        var lido = await Ler<RespostaProjeto>($"{Projetos}/{projeto.Id}");
        var ficheiro = await _cliente.GetAsync(imagem.Url);
        var bytes = await ficheiro.Content.ReadAsByteArrayAsync(Ct);

        Assert.Equal(HttpStatusCode.Created, envio.StatusCode);
        Assert.Equal("férias.JPG", imagem.NomeOriginal);
        Assert.Equal("image/jpeg", imagem.TipoMime);
        Assert.Single(lido.Imagens);
        Assert.Equal("image/jpeg", ficheiro.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", ficheiro.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.True(bytes.AsSpan().IndexOf("GPS-SECRETO"u8) < 0);
        Assert.Single(Directory.GetFiles(_api.PastaImagens));
    }

    [Theory]
    [InlineData("texto.jpg", "isto não é uma imagem")]
    [InlineData("desenho.svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    public async Task Um_ficheiro_que_nao_e_imagem_e_recusado_sem_deixar_nada_no_disco(string nome, string conteudo)
    {
        var projeto = await CriarProjeto();

        var resposta = await Enviar(projeto.Id, System.Text.Encoding.UTF8.GetBytes(conteudo), nome);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Empty(Directory.Exists(_api.PastaImagens) ? Directory.GetFiles(_api.PastaImagens) : []);
    }

    [Fact]
    public async Task Uma_imagem_acima_do_limite_e_recusada()
    {
        // Outra API, com outras chaves de cifra: a conta é criada de novo para ela.
        await using var api = new FabricaApi(bd, new() { ["Imagens:TamanhoMaximoBytes"] = "100" });
        await using (var contexto = bd.NovoBackoffice())
            await contexto.Database.ExecuteSqlRawAsync("DELETE FROM \"Utilizador\";", Ct);
        await api.CriarAdminAsync();
        var cliente = await api.NovoClienteComCsrfAsync();
        await api.EntrarComDoisFatoresAsync(cliente);
        var projeto = await (await cliente.PostAsync(Projetos, new { nome = "Grande" })).Content.ReadFromJsonAsync<RespostaProjeto>(Ct);

        var resposta = await Enviar(projeto!.Id, ImagensDeTeste.Criar(SKEncodedImageFormat.Png, 200, 200), "grande.png", cliente);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Reordena_e_remove_imagens_e_o_ficheiro_sai_do_disco()
    {
        var projeto = await CriarProjeto();
        var a = await EnviarImagem(projeto.Id);
        var b = await EnviarImagem(projeto.Id);

        var reordenar = await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/{projeto.Id}/imagens/ordem", new { ids = new[] { b.Id, a.Id } });
        var incompleta = await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/{projeto.Id}/imagens/ordem", new { ids = new[] { b.Id } });
        var ordem = (await Ler<RespostaProjeto>($"{Projetos}/{projeto.Id}")).Imagens.Select(i => i.Id);
        var remover = await _cliente.EnviarAsync(HttpMethod.Delete, $"/api/backoffice/imagens/{a.Id}");
        var depois = await _cliente.GetAsync(a.Url);

        Assert.Equal(HttpStatusCode.NoContent, reordenar.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, incompleta.StatusCode);
        Assert.Equal([b.Id, a.Id], ordem);
        Assert.Equal(HttpStatusCode.NoContent, remover.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
        Assert.Single(Directory.GetFiles(_api.PastaImagens));
    }

    [Fact]
    public async Task Apagar_um_projeto_apaga_os_ficheiros_das_imagens()
    {
        var projeto = await CriarProjeto();
        await EnviarImagem(projeto.Id);
        await EnviarImagem(projeto.Id);
        Assert.Equal(2, Directory.GetFiles(_api.PastaImagens).Length);

        var resposta = await _cliente.EnviarAsync(HttpMethod.Delete, $"{Projetos}/{projeto.Id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Empty(Directory.GetFiles(_api.PastaImagens));
    }

    [Fact]
    public async Task Enviar_para_um_projeto_inexistente_da_404()
    {
        var resposta = await Enviar(999, ImagensDeTeste.Criar(SKEncodedImageFormat.Png), "x.png");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task A_timeline_mostra_os_commits_do_repositorio()
    {
        var comRepositorio = await CriarProjeto("https://github.com/dono/repo");
        var semRepositorio = await CriarProjeto();

        var timeline = await Ler<RespostaTimeline>($"{Projetos}/{comRepositorio.Id}/timeline");
        var vazia = await Ler<RespostaTimeline>($"{Projetos}/{semRepositorio.Id}/timeline");
        var inexistente = await _cliente.GetAsync($"{Projetos}/999/timeline");
        var json = await (await _cliente.GetAsync($"{Projetos}/{comRepositorio.Id}/timeline")).Content.ReadAsStringAsync(Ct);

        Assert.Equal(EstadoTimeline.Atual, timeline.Estado);
        Assert.Equal(["Segundo commit", "Primeiro commit"], timeline.Commits.Select(c => c.Mensagem));
        Assert.Equal(EstadoTimeline.SemRepositorio, vazia.Estado);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Contains("\"estado\":\"atual\"", json);
        // As duas leituras do mesmo repositório usaram a cache.
        Assert.Equal(1, _github.Pedidos);
    }

    private async Task<RespostaProjeto> CriarProjeto(string? repositorio = null)
    {
        var resposta = await _cliente.PostAsync(Projetos, new { nome = "Projeto", repositorioGithub = repositorio });
        return (await resposta.Content.ReadFromJsonAsync<RespostaProjeto>(Ct))!;
    }

    private async Task<RespostaImagem> EnviarImagem(int projetoId)
    {
        var resposta = await Enviar(projetoId, ImagensDeTeste.Criar(SKEncodedImageFormat.Png), "imagem.png");
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<RespostaImagem>(Ct))!;
    }

    private Task<HttpResponseMessage> Enviar(int projetoId, byte[] conteudo, string nome, ClienteTeste? cliente = null)
    {
        var ficheiro = new ByteArrayContent(conteudo);
        ficheiro.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        var formulario = new MultipartFormDataContent { { ficheiro, "ficheiro", nome } };
        return (cliente ?? _cliente).EnviarAsync(HttpMethod.Post, $"{Projetos}/{projetoId}/imagens", formulario);
    }

    // Os estados da timeline vêm como texto ("atual"), como a API os envia.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private async Task<T> Ler<T>(string url) =>
        (await (await _cliente.GetAsync(url)).Content.ReadFromJsonAsync<T>(Json, Ct))!;
}
