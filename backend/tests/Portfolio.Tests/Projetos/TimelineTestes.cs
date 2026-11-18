using System.Net;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using Portfolio.Core.Projetos;
using Portfolio.Infrastructure.Github;

namespace Portfolio.Tests.Projetos;

// GitHub falso para os testes: conta os pedidos e pode falhar.
public sealed class GithubFalso : IGithub
{
    public int Pedidos { get; private set; }
    public bool Falhar { get; set; }
    public bool NaoExiste { get; set; }
    public List<Commit> Commits { get; } =
    [
        new("a".PadRight(40, 'a'), "Segundo commit", "Ana", DateTimeOffset.Parse("2026-11-02T10:00:00Z"), "https://github.com/dono/repo/commit/aaa"),
        new("b".PadRight(40, 'b'), "Primeiro commit", "Ana", DateTimeOffset.Parse("2026-11-01T10:00:00Z"), "https://github.com/dono/repo/commit/bbb"),
    ];

    public Task<IReadOnlyList<Commit>?> ObterCommitsAsync(LinkGithub repositorio, int quantidade, CancellationToken ct)
    {
        Pedidos++;
        if (Falhar)
            throw new HttpRequestException("GitHub em baixo");
        return Task.FromResult<IReadOnlyList<Commit>?>(NaoExiste ? null : Commits);
    }
}

public class CacheTimelineTests
{
    private static readonly LinkGithub Repositorio = new("dono", "repo");
    private readonly GithubFalso _github = new();
    private readonly FakeTimeProvider _relogio = new(DateTimeOffset.UtcNow);
    private readonly CacheTimeline _cache;

    public CacheTimelineTests() => _cache = new CacheTimeline(_github, _relogio, new OpcoesTimeline());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Durante_10_minutos_usa_a_cache()
    {
        var primeira = await _cache.ObterAsync(Repositorio, Ct);
        _relogio.Advance(TimeSpan.FromMinutes(9));
        await _cache.ObterAsync(Repositorio, Ct);
        _relogio.Advance(TimeSpan.FromMinutes(2));
        await _cache.ObterAsync(Repositorio, Ct);

        Assert.Equal(EstadoTimeline.Atual, primeira.Estado);
        Assert.Equal(2, primeira.Commits.Count);
        Assert.Equal(2, _github.Pedidos);
    }

    [Fact]
    public async Task Se_o_github_falhar_mostra_a_ultima_versao_e_espera_antes_de_tentar_de_novo()
    {
        await _cache.ObterAsync(Repositorio, Ct);
        _github.Falhar = true;
        _relogio.Advance(TimeSpan.FromMinutes(11));

        var desatualizada = await _cache.ObterAsync(Repositorio, Ct);
        await _cache.ObterAsync(Repositorio, Ct);
        _relogio.Advance(TimeSpan.FromMinutes(2));
        _github.Falhar = false;
        var recuperada = await _cache.ObterAsync(Repositorio, Ct);

        Assert.Equal(EstadoTimeline.Desatualizada, desatualizada.Estado);
        Assert.Equal(2, desatualizada.Commits.Count);
        Assert.Equal(EstadoTimeline.Atual, recuperada.Estado);
        // 1.ª boa, 1 falhada, a seguinte usa a espera de 1 minuto, depois a que recupera.
        Assert.Equal(3, _github.Pedidos);
    }

    [Fact]
    public async Task Sem_nada_em_cache_uma_falha_deixa_a_timeline_indisponivel()
    {
        _github.Falhar = true;

        var timeline = await _cache.ObterAsync(Repositorio, Ct);

        Assert.Equal(EstadoTimeline.Indisponivel, timeline.Estado);
        Assert.Empty(timeline.Commits);
    }

    [Fact]
    public async Task Um_repositorio_que_nao_existe_e_indicado()
    {
        _github.NaoExiste = true;

        var timeline = await _cache.ObterAsync(Repositorio, Ct);

        Assert.Equal(EstadoTimeline.RepositorioNaoEncontrado, timeline.Estado);
    }
}

public class GithubApiTests
{
    private static readonly LinkGithub Repositorio = new("dono", "meu.repo");
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Le_os_commits_com_a_primeira_linha_e_o_link_montado_pela_api()
    {
        var sha = new string('c', 40);
        var json = $$"""
            [
              { "sha": "{{sha}}", "html_url": "https://evil.example/x",
                "commit": { "message": "Corrige o login\n\nDetalhes compridos", "author": { "name": "Ana", "date": "2026-11-18T10:00:00Z" } } },
              { "sha": "não-é-um-sha", "commit": { "message": "Ignorado" } }
            ]
            """;
        var (api, manipulador) = Criar(HttpStatusCode.OK, json);

        var commits = await api.ObterCommitsAsync(Repositorio, 100, Ct);

        var commit = Assert.Single(commits!);
        Assert.Equal("Corrige o login", commit.Mensagem);
        Assert.Equal("Ana", commit.Autor);
        Assert.Equal($"https://github.com/dono/meu.repo/commit/{sha}", commit.Url);
        Assert.Equal("https://api.github.com/repos/dono/meu.repo/commits?per_page=100", manipulador.Pedido!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Um_repositorio_que_nao_existe_devolve_null_e_um_vazio_devolve_lista_vazia()
    {
        var (naoExiste, _) = Criar(HttpStatusCode.NotFound, "{}");
        var (vazio, _) = Criar(HttpStatusCode.Conflict, "{}");

        Assert.Null(await naoExiste.ObterCommitsAsync(Repositorio, 100, Ct));
        Assert.Empty((await vazio.ObterCommitsAsync(Repositorio, 100, Ct))!);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.Forbidden, "{\"message\":\"API rate limit exceeded\"}")]
    [InlineData(HttpStatusCode.OK, "isto não é json")]
    public async Task Falhas_do_github_lancam_HttpRequestException(HttpStatusCode estado, string corpo)
    {
        var (api, _) = Criar(estado, corpo);

        await Assert.ThrowsAsync<HttpRequestException>(() => api.ObterCommitsAsync(Repositorio, 100, Ct));
    }

    private static (GithubApi, Manipulador) Criar(HttpStatusCode estado, string corpo)
    {
        var manipulador = new Manipulador(estado, corpo);
        return (new GithubApi(new Fabrica(manipulador)), manipulador);
    }

    private sealed class Manipulador(HttpStatusCode estado, string corpo) : HttpMessageHandler
    {
        public HttpRequestMessage? Pedido { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken ct)
        {
            Pedido = pedido;
            return Task.FromResult(new HttpResponseMessage(estado) { Content = new StringContent(corpo, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class Fabrica(HttpMessageHandler manipulador) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(manipulador) { BaseAddress = new Uri("https://api.github.com/") };
    }
}
