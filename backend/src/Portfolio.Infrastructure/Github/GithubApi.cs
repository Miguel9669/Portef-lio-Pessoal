using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Portfolio.Core.Projetos;

namespace Portfolio.Infrastructure.Github;

// Commits pela API REST do GitHub (SEG29). O cliente HTTP "github" está configurado no
// DependencyInjection com o endereço fixo https://api.github.com, User-Agent, tempo limite
// e o token opcional. É pedido à fábrica em cada chamada, para não ficar com ligações antigas.
public sealed partial class GithubApi(IHttpClientFactory fabrica) : IGithub
{
    public const string Cliente = "github";
    private const int TamanhoMaximoMensagem = 500;

    public async Task<IReadOnlyList<Commit>?> ObterCommitsAsync(LinkGithub repositorio, int quantidade, CancellationToken ct)
    {
        // O endereço é montado aqui, a partir do dono e do repositório já validados,
        // e não a partir do link escrito no backoffice.
        var endereco = $"repos/{Uri.EscapeDataString(repositorio.Dono)}/{Uri.EscapeDataString(repositorio.Repositorio)}/commits?per_page={quantidade}";
        var http = fabrica.CreateClient(Cliente);
        using var resposta = await http.GetAsync(endereco, ct);

        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return null;

        // 409: o repositório existe mas ainda não tem commits.
        if (resposta.StatusCode == HttpStatusCode.Conflict)
            return [];

        resposta.EnsureSuccessStatusCode();
        try
        {
            var commits = await resposta.Content.ReadFromJsonAsync<List<CommitGithub>>(ct) ?? [];
            return commits.Where(c => Sha().IsMatch(c.Sha ?? "")).Select(c => Converter(c, repositorio)).ToList();
        }
        catch (JsonException erro)
        {
            throw new HttpRequestException("O GitHub devolveu uma resposta inválida.", erro);
        }
    }

    // O link do commit é montado aqui, não copiado da resposta: o frontend só recebe links do github.com.
    private static Commit Converter(CommitGithub c, LinkGithub repositorio) => new(
        c.Sha!,
        PrimeiraLinha(c.Commit?.Message),
        c.Commit?.Author?.Name ?? "",
        c.Commit?.Author?.Date ?? DateTimeOffset.MinValue,
        $"{repositorio.Url}/commit/{c.Sha}");

    private static string PrimeiraLinha(string? mensagem)
    {
        var linha = (mensagem ?? "").Split('\n', 2)[0].Trim();
        return linha.Length <= TamanhoMaximoMensagem ? linha : linha[..TamanhoMaximoMensagem];
    }

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex Sha();

    private sealed record CommitGithub(
        [property: JsonPropertyName("sha")] string? Sha,
        [property: JsonPropertyName("commit")] DadosCommit? Commit);

    private sealed record DadosCommit(
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("author")] AutorCommit? Author);

    private sealed record AutorCommit(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("date")] DateTimeOffset? Date);
}
