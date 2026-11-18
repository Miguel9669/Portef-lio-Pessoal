using AngleSharp.Html.Parser;
using Portfolio.Core.Comum;
using Portfolio.Core.Projetos;
using Portfolio.Infrastructure.Conteudo;

namespace Portfolio.Tests.Conteudo;

public class SlugTests
{
    [Theory]
    [InlineData("Sobre mim", "sobre-mim")]
    [InlineData("Portfólio: Ação & Reação!", "portfolio-acao-reacao")]
    [InlineData("  --Olá   Mundo--  ", "ola-mundo")]
    [InlineData("C# e .NET 10", "c-e-net-10")]
    [InlineData("!!!", "predefinido")]
    public void Gera_slugs_sem_acentos_nem_simbolos(string texto, string esperado)
    {
        Assert.Equal(esperado, Slug.Gerar(texto, "predefinido"));
    }

    [Fact]
    public void Corta_os_slugs_compridos()
    {
        var slug = Slug.Gerar(new string('a', 200), "x");

        Assert.Equal(Slug.TamanhoMaximo, slug.Length);
    }

    [Fact]
    public void Um_slug_repetido_ganha_um_sufixo()
    {
        Assert.Equal("projeto", Slug.Unico("projeto", ["projeto-2"]));
        Assert.Equal("projeto-2", Slug.Unico("projeto", ["projeto"]));
        Assert.Equal("projeto-3", Slug.Unico("projeto", ["projeto", "projeto-2"]));
    }
}

public class LinkGithubTests
{
    [Theory]
    [InlineData("https://github.com/Miguel9669/Portef-lio-Pessoal")]
    [InlineData("http://github.com/Miguel9669/Portef-lio-Pessoal")]
    [InlineData("github.com/Miguel9669/Portef-lio-Pessoal")]
    [InlineData("https://www.github.com/Miguel9669/Portef-lio-Pessoal/")]
    [InlineData("https://github.com/Miguel9669/Portef-lio-Pessoal.git")]
    [InlineData("  https://GitHub.com/Miguel9669/Portef-lio-Pessoal  ")]
    public void Aceita_variacoes_e_devolve_a_forma_canonica(string texto)
    {
        var link = LinkGithub.Ler(texto);

        Assert.Equal("https://github.com/Miguel9669/Portef-lio-Pessoal", link?.Url);
        Assert.Equal("Miguel9669", link!.Dono);
        Assert.Equal("Portef-lio-Pessoal", link.Repositorio);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://gitlab.com/dono/repositorio")]
    [InlineData("https://github.com.evil.example/dono/repositorio")]
    [InlineData("https://evil.example/github.com/dono/repositorio")]
    [InlineData("https://github.com/dono")]
    [InlineData("https://github.com/dono/repositorio/tree/main")]
    [InlineData("https://github.com/dono/..")]
    [InlineData("https://github.com/dono/.")]
    [InlineData("https://github.com/dono/repo?x=1")]
    [InlineData("https://user@github.com/dono/repositorio")]
    [InlineData("javascript:alert(1)")]
    public void Recusa_tudo_o_que_nao_for_um_repositorio_do_github(string? texto)
    {
        Assert.Null(LinkGithub.Ler(texto));
    }
}

public class ConversorMarkdigTests
{
    private readonly ConversorMarkdig _conversor = new();

    [Fact]
    public void Converte_o_markdown_basico()
    {
        var html = _conversor.ParaHtml("# Título\n\nTexto **forte** e *itálico*.\n\n- um\n- dois");

        Assert.Contains("<h1", html);
        Assert.Contains("<strong>forte</strong>", html);
        Assert.Contains("<em>itálico</em>", html);
        Assert.Contains("<li>um</li>", html);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("Olá <img src=x onerror=alert(1)>")]
    [InlineData("<iframe src=\"https://evil.example\"></iframe>")]
    [InlineData("<a href=\"https://exemplo.pt\" onclick=\"alert(1)\">x</a>")]
    public void O_html_escrito_no_texto_nunca_sai_como_html(string markdown)
    {
        // Analisa o HTML como um browser: o texto escapado (&lt;img ...&gt;) é inofensivo,
        // o que não pode existir são elementos ou atributos de eventos.
        var documento = new HtmlParser().ParseDocument(_conversor.ParaHtml(markdown));
        var elementos = documento.Body!.QuerySelectorAll("*");

        Assert.Empty(documento.QuerySelectorAll("script, img, iframe"));
        Assert.DoesNotContain(elementos.SelectMany(e => e.Attributes), a => a.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("[clica](javascript:alert(1))")]
    [InlineData("[clica](JaVaScRiPt:alert(1))")]
    [InlineData("[clica](data:text/html;base64,PHNjcmlwdD4=)")]
    [InlineData("[clica](vbscript:msgbox(1))")]
    public void Links_perigosos_perdem_o_destino(string markdown)
    {
        var html = _conversor.ParaHtml(markdown);

        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:", html);
        Assert.DoesNotContain("vbscript:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Os_links_levam_noopener_noreferrer()
    {
        var html = _conversor.ParaHtml("[GitHub](https://github.com)");

        Assert.Contains("href=\"https://github.com\"", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
    }
}
