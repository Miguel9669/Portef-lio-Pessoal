using Portfolio.Core.Comum;
using Portfolio.Core.Conteudo;
using Portfolio.Core.Entidades;
using Portfolio.Core.Imagens;
using Portfolio.Core.Projetos;

namespace Portfolio.Core.SitePublico;

// Só leitura, pelo PublicoDbContext (utilizador portfolio_leitura, SEG01, SEG02).
// Todas as consultas já filtram por Publico = true (SEG16): o service nunca vê o resto.
public interface IRepositorioPublico
{
    // Por ordem.
    Task<List<BlocoConteudo>> ListarBlocosAsync(CancellationToken ct);

    // Do mais recente para o mais antigo, com as imagens.
    Task<List<Projeto>> ListarProjetosAsync(CancellationToken ct);

    Task<Projeto?> ObterProjetoAsync(string slug, CancellationToken ct);

    // Só se a imagem for de um projeto público.
    Task<ImagemProjeto?> ObterImagemAsync(string ficheiro, CancellationToken ct);
}

public sealed record BlocoPublico(string Titulo, string Slug, string Html);

// O que o site precisa de mostrar e mais nada (sem ids internos nem datas de edição).
// RepositorioGithub só vem se o código for público; TemHistorico diz se há timeline no site.
public sealed record ProjetoPublico(
    string Nome,
    string Slug,
    string? Descricao,
    string? RepositorioGithub,
    string? LinkOficial,
    bool TemHistorico,
    DateTime CriadoEm,
    IReadOnlyList<ImagemPublica> Imagens);

public sealed record ImagemPublica(string Url, string NomeOriginal);

// Conteúdo do site público (RF05, RF09, RF12).
public sealed class SitePublicoService(
    IRepositorioPublico repositorio,
    IConversorMarkdown markdown,
    CacheTimeline timeline,
    IArmazenamentoImagens armazenamento)
{
    public const string PrefixoImagens = "/api/publico/imagens/";

    // O Markdown é convertido aqui, no servidor, e o site mostra o HTML já sanitizado (SEG21).
    public async Task<List<BlocoPublico>> BlocosAsync(CancellationToken ct) =>
        (await repositorio.ListarBlocosAsync(ct)).Select(b => new BlocoPublico(b.Titulo, b.Slug, markdown.ParaHtml(b.Conteudo))).ToList();

    public async Task<List<ProjetoPublico>> ProjetosAsync(CancellationToken ct) =>
        (await repositorio.ListarProjetosAsync(ct)).Select(Converter).ToList();

    public async Task<Resultado<ProjetoPublico>> ProjetoAsync(string slug, CancellationToken ct) =>
        await repositorio.ObterProjetoAsync(slug, ct) is { } projeto ? Converter(projeto) : NaoEncontrado();

    public async Task<Resultado<Timeline>> TimelineAsync(string slug, CancellationToken ct)
    {
        var projeto = await repositorio.ObterProjetoAsync(slug, ct);
        if (projeto is null)
            return NaoEncontrado();

        // Repositório privado sem o histórico ligado: nem se pede ao GitHub (SEG29).
        return projeto.MostraHistorico && LinkGithub.Ler(projeto.RepositorioGithub) is { } link
            ? await timeline.ObterAsync(link, ct)
            : Timeline.SemRepositorio;
    }

    // SEG16, SEG20: o ficheiro só sai se estiver na base de dados como imagem de um projeto público.
    // O nome pedido nunca chega ao disco sem ter sido encontrado na base de dados.
    public async Task<Resultado<(string TipoMime, Stream Conteudo)>> ImagemAsync(string ficheiro, CancellationToken ct)
    {
        var imagem = await repositorio.ObterImagemAsync(ficheiro, ct);
        if (imagem is null || armazenamento.Abrir(imagem.Ficheiro) is not { } conteudo)
            return Erro.NaoEncontrado("Imagem não encontrada.");
        return (imagem.TipoMime, conteudo);
    }

    private static ProjetoPublico Converter(Projeto p) => new(
        p.Nome, p.Slug, p.Descricao, p.MostraCodigo ? p.RepositorioGithub : null, p.LinkOficial, p.MostraHistorico, p.CriadoEm,
        p.Imagens.OrderBy(i => i.Ordem).Select(i => new ImagemPublica(PrefixoImagens + i.Ficheiro, i.NomeOriginal)).ToList());

    private static Erro NaoEncontrado() => Erro.NaoEncontrado("Projeto não encontrado.");
}
