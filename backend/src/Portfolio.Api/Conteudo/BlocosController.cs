using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Configuracao;
using Portfolio.Core.Conteudo;
using Portfolio.Core.Entidades;

namespace Portfolio.Api.Conteudo;

public sealed record PedidoBloco(
    [Required, MaxLength(150)] string Titulo,
    [Required(AllowEmptyStrings = true), MaxLength(PedidoBloco.TamanhoMaximoConteudo)] string Conteudo,
    bool Publico)
{
    public const int TamanhoMaximoConteudo = 100_000;
}

public sealed record PedidoOrdemBlocos([Required] int[] Ids);

public sealed record RespostaBloco(
    int Id, string Titulo, string Slug, string Conteudo, int Ordem, bool Publico, DateTime CriadoEm, DateTime AtualizadoEm)
{
    public static RespostaBloco De(BlocoConteudo b) =>
        new(b.Id, b.Titulo, b.Slug, b.Conteudo, b.Ordem, b.Publico, b.CriadoEm, b.AtualizadoEm);
}

// Blocos de conteúdo do site (RF04). Login completo obrigatório (FallbackPolicy, SEG13).
[ApiController]
[Route("api/backoffice/blocos")]
public sealed class BlocosController(BlocoConteudoService blocos) : ControllerBase
{
    // Todos, incluindo os ocultos, por ordem.
    [HttpGet]
    public async Task<IEnumerable<RespostaBloco>> Listar(CancellationToken ct) =>
        (await blocos.ListarAsync(ct)).Select(RespostaBloco.De);

    [HttpPost]
    public async Task<ActionResult<RespostaBloco>> Criar(PedidoBloco pedido, CancellationToken ct)
    {
        var bloco = await blocos.CriarAsync(Dados(pedido), ct);
        return Created($"/api/backoffice/blocos/{bloco.Id}", RespostaBloco.De(bloco));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RespostaBloco>> Editar(int id, PedidoBloco pedido, CancellationToken ct)
    {
        var resultado = await blocos.EditarAsync(id, Dados(pedido), ct);
        return resultado.Erro is { } erro ? this.Para(erro) : RespostaBloco.De(resultado.Valor!);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Remover(int id, CancellationToken ct) =>
        await blocos.RemoverAsync(id, ct) is { } erro ? this.Para(erro) : NoContent();

    // A nova ordem, depois de arrastar: os ids de todos os blocos, do primeiro ao último.
    [HttpPut("ordem")]
    public async Task<ActionResult> Reordenar(PedidoOrdemBlocos pedido, CancellationToken ct) =>
        await blocos.ReordenarAsync(pedido.Ids, ct) is { } erro ? this.Para(erro) : NoContent();

    private static DadosBloco Dados(PedidoBloco p) => new(p.Titulo, p.Conteudo, p.Publico);
}
