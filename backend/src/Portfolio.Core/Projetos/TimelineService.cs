using System.Collections.Concurrent;
using Portfolio.Core.Comum;

namespace Portfolio.Core.Projetos;

// Um commit da timeline (RF11, RF12). Tudo é texto vindo do GitHub: os frontends
// mostram-no como texto, nunca como HTML (SEG29).
public sealed record Commit(string Sha, string Mensagem, string Autor, DateTimeOffset Data, string Url);

// Lê os commits na API do GitHub (SEG29). Devolve null se o repositório não existir
// (ou for privado e não houver acesso). Lança uma exceção se o GitHub não responder.
public interface IGithub
{
    Task<IReadOnlyList<Commit>?> ObterCommitsAsync(LinkGithub repositorio, int quantidade, CancellationToken ct);
}

public sealed class OpcoesTimeline
{
    public int Quantidade { get; set; } = 100;
    public TimeSpan Cache { get; set; } = TimeSpan.FromMinutes(10);

    // Depois de uma falha do GitHub, espera-se este tempo antes de tentar de novo.
    public TimeSpan EsperaAposFalha { get; set; } = TimeSpan.FromMinutes(1);
}

public enum EstadoTimeline
{
    Atual,

    // O GitHub falhou na última tentativa: são os commits da última vez que respondeu.
    Desatualizada,

    // O GitHub falhou e não há nada em cache.
    Indisponivel,

    RepositorioNaoEncontrado,
    SemRepositorio,
}

public sealed record Timeline(EstadoTimeline Estado, IReadOnlyList<Commit> Commits, DateTimeOffset? AtualizadoEm)
{
    public static readonly Timeline SemRepositorio = new(EstadoTimeline.SemRepositorio, [], null);
}

// Cache dos commits por repositório, partilhada por todos os pedidos (singleton).
// Guarda sempre a última resposta boa, para a mostrar se o GitHub falhar.
public sealed class CacheTimeline(IGithub github, TimeProvider relogio, OpcoesTimeline opcoes)
{
    private sealed record Entrada(IReadOnlyList<Commit>? Commits, DateTimeOffset? AtualizadoEm, bool UltimaFalhou, bool NaoEncontrado, DateTimeOffset ProximaTentativa);

    private readonly ConcurrentDictionary<string, Entrada> _entradas = new(StringComparer.OrdinalIgnoreCase);

    public async Task<Timeline> ObterAsync(LinkGithub repositorio, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        if (!_entradas.TryGetValue(repositorio.Url, out var entrada) || agora >= entrada.ProximaTentativa)
            entrada = _entradas[repositorio.Url] = await PedirAoGithubAsync(repositorio, entrada, agora, ct);

        return ParaTimeline(entrada);
    }

    private async Task<Entrada> PedirAoGithubAsync(LinkGithub repositorio, Entrada? anterior, DateTimeOffset agora, CancellationToken ct)
    {
        try
        {
            var commits = await github.ObterCommitsAsync(repositorio, opcoes.Quantidade, ct);
            return new Entrada(commits ?? [], agora, UltimaFalhou: false, NaoEncontrado: commits is null, agora + opcoes.Cache);
        }
        catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return new Entrada(anterior?.Commits, anterior?.AtualizadoEm, UltimaFalhou: true, NaoEncontrado: false, agora + opcoes.EsperaAposFalha);
        }
    }

    private static Timeline ParaTimeline(Entrada entrada) => entrada switch
    {
        { NaoEncontrado: true } => new(EstadoTimeline.RepositorioNaoEncontrado, [], entrada.AtualizadoEm),
        { UltimaFalhou: true, Commits: null } => new(EstadoTimeline.Indisponivel, [], null),
        { UltimaFalhou: true } => new(EstadoTimeline.Desatualizada, entrada.Commits!, entrada.AtualizadoEm),
        _ => new(EstadoTimeline.Atual, entrada.Commits!, entrada.AtualizadoEm),
    };
}

// Timeline de um projeto (RF11, RF12).
public sealed class TimelineService(IRepositorioProjetos projetos, CacheTimeline cache)
{
    // Backoffice: qualquer projeto, público ou não.
    public async Task<Resultado<Timeline>> ObterAsync(int projetoId, CancellationToken ct)
    {
        var projeto = await projetos.ObterAsync(projetoId, ct);
        if (projeto is null)
            return Erro.NaoEncontrado("Projeto não encontrado.");

        return LinkGithub.Ler(projeto.RepositorioGithub) is { } link
            ? await cache.ObterAsync(link, ct)
            : Timeline.SemRepositorio;
    }
}
