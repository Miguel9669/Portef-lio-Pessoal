using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Configuracao;
using Portfolio.Core.Entidades;
using Portfolio.Core.Imagens;

namespace Portfolio.Api.Projetos;

public sealed record RespostaImagem(int Id, string NomeOriginal, string TipoMime, long TamanhoBytes, int Ordem, string Url)
{
    public static RespostaImagem De(ImagemProjeto i) =>
        new(i.Id, i.NomeOriginal, i.TipoMime, i.TamanhoBytes, i.Ordem, $"/api/backoffice/imagens/{i.Id}");
}

public sealed record PedidoOrdemImagens([Required] int[] Ids);

// Imagens dos projetos no backoffice (RF13, SEG17–SEG20). Login completo obrigatório (SEG13).
[ApiController]
public sealed class ImagensController(ImagemService imagens) : ControllerBase
{
    // SEG18: o pedido inteiro (imagem + multipart) não passa de 6 MB; a imagem em si,
    // de Imagens:TamanhoMaximoBytes (5 MB), verificado no ImagemService.
    public const long TamanhoMaximoPedido = 6 * 1024 * 1024;

    [HttpPost("api/backoffice/projetos/{projetoId:int}/imagens")]
    [RequestSizeLimit(TamanhoMaximoPedido)]
    [RequestFormLimits(MultipartBodyLengthLimit = TamanhoMaximoPedido)]
    public async Task<ActionResult<RespostaImagem>> Enviar(int projetoId, [Required] IFormFile ficheiro, CancellationToken ct)
    {
        await using var conteudo = ficheiro.OpenReadStream();
        var resultado = await imagens.EnviarAsync(projetoId, conteudo, ficheiro.FileName, ficheiro.Length, ct);
        if (resultado.Erro is { } erro)
            return this.Para(erro);

        var imagem = RespostaImagem.De(resultado.Valor!);
        return Created(imagem.Url, imagem);
    }

    [HttpPut("api/backoffice/projetos/{projetoId:int}/imagens/ordem")]
    public async Task<ActionResult> Reordenar(int projetoId, PedidoOrdemImagens pedido, CancellationToken ct) =>
        await imagens.ReordenarAsync(projetoId, pedido.Ids, ct) is { } erro ? this.Para(erro) : NoContent();

    [HttpDelete("api/backoffice/imagens/{id:int}")]
    public async Task<ActionResult> Remover(int id, CancellationToken ct) =>
        await imagens.RemoverAsync(id, ct) is { } erro ? this.Para(erro) : NoContent();

    // Para o backoffice mostrar as imagens, incluindo as de projetos não públicos.
    // O tipo vem da base de dados (definido ao processar), nunca do que o cliente enviou;
    // o nosniff vem dos cabeçalhos de segurança (SEG20, SEG23).
    [HttpGet("api/backoffice/imagens/{id:int}")]
    public async Task<ActionResult> Ver(int id, CancellationToken ct)
    {
        var resultado = await imagens.AbrirAsync(id, ct);
        if (resultado.Erro is { } erro)
            return this.Para(erro);

        var (imagem, conteudo) = resultado.Valor;
        return File(conteudo, imagem.TipoMime);
    }
}
