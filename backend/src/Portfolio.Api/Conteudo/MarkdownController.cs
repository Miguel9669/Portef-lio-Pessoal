using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Core.Conteudo;

namespace Portfolio.Api.Conteudo;

public sealed record PedidoMarkdown([Required(AllowEmptyStrings = true), MaxLength(PedidoBloco.TamanhoMaximoConteudo)] string Texto);

public sealed record RespostaMarkdown(string Html);

// Pré-visualização no editor do backoffice: o mesmo HTML sanitizado que o site vai mostrar (SEG21).
[ApiController]
[Route("api/backoffice/markdown")]
public sealed class MarkdownController(IConversorMarkdown conversor) : ControllerBase
{
    [HttpPost("pre-visualizar")]
    public RespostaMarkdown PreVisualizar(PedidoMarkdown pedido) => new(conversor.ParaHtml(pedido.Texto));
}
