using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Portfolio.Api.Configuracao;
using Portfolio.Api.Projetos;
using Portfolio.Core.SitePublico;

namespace Portfolio.Api.SitePublico;

// Site público (RF05, RF09, RF12): sem login e só GET (SEG14). Aberto explicitamente com
// [AllowAnonymous], porque tudo o resto é fechado por defeito (SEG13).
[ApiController]
[Route("api/publico")]
[AllowAnonymous]
[EnableRateLimiting(LimitePublico.Politica)]
public sealed class SitePublicoController(SitePublicoService site) : ControllerBase
{
    [HttpGet("blocos")]
    public Task<List<BlocoPublico>> Blocos(CancellationToken ct) => site.BlocosAsync(ct);

    [HttpGet("projetos")]
    public Task<List<ProjetoPublico>> Projetos(CancellationToken ct) => site.ProjetosAsync(ct);

    [HttpGet("projetos/{slug}")]
    public async Task<ActionResult<ProjetoPublico>> Projeto(string slug, CancellationToken ct)
    {
        var resultado = await site.ProjetoAsync(slug, ct);
        return resultado.Erro is { } erro ? this.Para(erro) : resultado.Valor!;
    }

    // Os commits são texto vindo do GitHub: o site mostra-os como texto, nunca como HTML (SEG29).
    [HttpGet("projetos/{slug}/timeline")]
    public async Task<ActionResult<RespostaTimeline>> Timeline(string slug, CancellationToken ct)
    {
        var resultado = await site.TimelineAsync(slug, ct);
        if (resultado.Erro is { } erro)
            return this.Para(erro);

        var t = resultado.Valor!;
        return new RespostaTimeline(t.Estado, t.AtualizadoEm, t.Commits);
    }

    // SEG16, SEG20. O nome do ficheiro é único e nunca muda de conteúdo, por isso pode ficar
    // em cache no browser. O nosniff vem dos cabeçalhos de segurança (SEG23).
    [HttpGet("imagens/{ficheiro}")]
    public async Task<ActionResult> Imagem(string ficheiro, CancellationToken ct)
    {
        var resultado = await site.ImagemAsync(ficheiro, ct);
        if (resultado.Erro is { } erro)
            return this.Para(erro);

        var (tipo, conteudo) = resultado.Valor;
        Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
        return File(conteudo, tipo);
    }
}

// Rate limit geral do site público: por defeito 100 pedidos por minuto por IP (docs/03-seguranca.md).
public sealed class LimitePublico
{
    public const string Politica = "publico";

    public int PedidosPorMinutoPorIp { get; set; } = 100;
}
