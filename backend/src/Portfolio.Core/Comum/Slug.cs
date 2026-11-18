using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Portfolio.Core.Comum;

// Slugs dos URLs públicos (/projetos/meu-projeto) e das âncoras do site (/#sobre-mim).
public static partial class Slug
{
    // A coluna tem 160 caracteres; o resto fica para o sufixo de repetidos (-2, -3…).
    public const int TamanhoMaximo = 150;

    // "Portfólio: Ação & Reação!" -> "portfolio-acao-reacao"
    public static string Gerar(string texto, string predefinido)
    {
        var semAcentos = new StringBuilder(texto.Length);
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                semAcentos.Append(c);
        }

        var slug = NaoAlfanumerico().Replace(semAcentos.ToString().ToLowerInvariant(), "-").Trim('-');
        if (slug.Length > TamanhoMaximo)
            slug = slug[..TamanhoMaximo].TrimEnd('-');
        return slug.Length == 0 ? predefinido : slug;
    }

    // Se o slug já existir, acrescenta -2, -3… até encontrar um livre.
    public static string Unico(string slug, IReadOnlyCollection<string> existentes)
    {
        var candidato = slug;
        for (var n = 2; existentes.Contains(candidato); n++)
            candidato = $"{slug}-{n}";
        return candidato;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NaoAlfanumerico();
}
