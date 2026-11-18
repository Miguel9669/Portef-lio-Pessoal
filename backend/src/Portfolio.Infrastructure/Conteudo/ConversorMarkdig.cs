using AngleSharp.Html.Dom;
using Ganss.Xss;
using Markdig;
using Portfolio.Core.Conteudo;

namespace Portfolio.Infrastructure.Conteudo;

// Markdown para HTML seguro (SEG21), em duas camadas:
// 1. O Markdig corre com o HTML embutido desativado: um <script> escrito no texto sai como texto.
// 2. O HTML gerado passa pelo HtmlSanitizer, que só deixa etiquetas seguras e remove links
//    javascript: e afins. Os links levam rel="noopener noreferrer".
public sealed class ConversorMarkdig : IConversorMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    private readonly HtmlSanitizer _sanitizador = CriarSanitizador();

    public string ParaHtml(string markdown) => _sanitizador.Sanitize(Markdown.ToHtml(markdown, Pipeline));

    private static HtmlSanitizer CriarSanitizador()
    {
        var sanitizador = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            // Parágrafos, títulos, listas, negrito, itálico, código, citações e links.
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li",
                "strong", "em", "code", "pre", "blockquote", "a", "br", "hr",
            },
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto" },
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" },
        });

        sanitizador.PostProcessNode += (_, e) =>
        {
            if (e.Node is IHtmlAnchorElement link)
                link.SetAttribute("rel", "noopener noreferrer");
        };
        return sanitizador;
    }
}
