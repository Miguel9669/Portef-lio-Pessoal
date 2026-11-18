using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Configuracao;
using Portfolio.Core.Comum;
using Portfolio.Core.Entidades;
using Portfolio.Core.Projetos;

namespace Portfolio.Api.Projetos;

// RepositorioGithub: opcional; se vier, tem de ser o link de um repositório do GitHub (SEG29).
public sealed record PedidoProjeto(
    [Required, MaxLength(150)] string Nome,
    [MaxLength(10_000)] string? Descricao,
    [MaxLength(250)] string? RepositorioGithub);

public sealed record PedidoPublicar(bool Publico);

public sealed record RespostaProjeto(
    int Id, string Nome, string Slug, string? Descricao, string? RepositorioGithub, bool Publico,
    DateTime CriadoEm, DateTime AtualizadoEm)
{
    public static RespostaProjeto De(Projeto p) =>
        new(p.Id, p.Nome, p.Slug, p.Descricao, p.RepositorioGithub, p.Publico, p.CriadoEm, p.AtualizadoEm);
}

// Projetos no backoffice (RF06–RF08, RF10). Login completo obrigatório (FallbackPolicy, SEG13).
[ApiController]
[Route("api/backoffice/projetos")]
public sealed class ProjetosController(ProjetoService projetos) : ControllerBase
{
    // Todos, públicos e não públicos, do mais recente para o mais antigo.
    [HttpGet]
    public async Task<IEnumerable<RespostaProjeto>> Listar(CancellationToken ct) =>
        (await projetos.ListarAsync(ct)).Select(RespostaProjeto.De);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RespostaProjeto>> Obter(int id, CancellationToken ct) =>
        Responder(await projetos.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<RespostaProjeto>> Criar(PedidoProjeto pedido, CancellationToken ct)
    {
        var resultado = await projetos.CriarAsync(Dados(pedido), ct);
        return resultado.Erro is { } erro
            ? this.Para(erro)
            : Created($"/api/backoffice/projetos/{resultado.Valor!.Id}", RespostaProjeto.De(resultado.Valor));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RespostaProjeto>> Editar(int id, PedidoProjeto pedido, CancellationToken ct) =>
        Responder(await projetos.EditarAsync(id, Dados(pedido), ct));

    // RF08
    [HttpPut("{id:int}/publico")]
    public async Task<ActionResult<RespostaProjeto>> Publicar(int id, PedidoPublicar pedido, CancellationToken ct) =>
        Responder(await projetos.PublicarAsync(id, pedido.Publico, ct));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Remover(int id, CancellationToken ct) =>
        await projetos.RemoverAsync(id, ct) is { } erro ? this.Para(erro) : NoContent();

    private ActionResult<RespostaProjeto> Responder(Resultado<Projeto> resultado) =>
        resultado.Erro is { } erro ? this.Para(erro) : RespostaProjeto.De(resultado.Valor!);

    private static DadosProjeto Dados(PedidoProjeto p) => new(p.Nome, p.Descricao, p.RepositorioGithub);
}
