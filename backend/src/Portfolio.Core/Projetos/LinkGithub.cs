using System.Text.RegularExpressions;

namespace Portfolio.Core.Projetos;

// Link do repositório de um projeto no GitHub (RF10, SEG29).
// A API só usa o dono e o repositório para montar o endereço da API do GitHub,
// nunca o texto escrito, por isso não pode ser levada a pedir outros servidores.
public sealed partial record LinkGithub(string Dono, string Repositorio)
{
    // A forma gravada na base de dados (tem um CHECK com este formato).
    public string Url => $"https://github.com/{Dono}/{Repositorio}";

    // Aceita com ou sem https:// e www., e com / ou .git no fim, e devolve a forma canónica.
    // Devolve null se não for o link de um repositório do GitHub.
    public static LinkGithub? Ler(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var correspondencia = Formato().Match(texto.Trim());
        if (!correspondencia.Success)
            return null;

        var repositorio = correspondencia.Groups["repositorio"].Value;
        if (repositorio.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repositorio = repositorio[..^4];

        // "." e ".." passariam no formato, mas não são repositórios.
        return repositorio is "" or "." or ".."
            ? null
            : new LinkGithub(correspondencia.Groups["dono"].Value, repositorio);
    }

    // Nomes do GitHub: dono até 39 caracteres (letras, números e hífenes),
    // repositório até 100 (letras, números, ".", "_" e "-").
    [GeneratedRegex(@"^(?:https?://)?(?:www\.)?github\.com/(?<dono>[A-Za-z0-9-]{1,39})/(?<repositorio>[A-Za-z0-9._-]{1,100})/?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Formato();
}
