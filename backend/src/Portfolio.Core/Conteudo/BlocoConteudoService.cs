using Portfolio.Core.Comum;
using Portfolio.Core.Entidades;

namespace Portfolio.Core.Conteudo;

// Markdown para HTML sanitizado (SEG21).
public interface IConversorMarkdown
{
    string ParaHtml(string markdown);
}

public interface IRepositorioBlocos
{
    // Por ordem.
    Task<List<BlocoConteudo>> ListarAsync(CancellationToken ct);
    Task<BlocoConteudo?> ObterAsync(int id, CancellationToken ct);
    Task<List<string>> SlugsComecadosPorAsync(string inicio, CancellationToken ct);
    void Adicionar(BlocoConteudo bloco);
    void Remover(BlocoConteudo bloco);
    Task GravarAsync(CancellationToken ct);
}

public sealed record DadosBloco(string Titulo, string Conteudo, bool Publico);

// Blocos de conteúdo do site no backoffice (RF04).
public sealed class BlocoConteudoService(IRepositorioBlocos blocos)
{
    public Task<List<BlocoConteudo>> ListarAsync(CancellationToken ct) => blocos.ListarAsync(ct);

    // Um bloco novo vai para o fim da página. O slug (âncora no site) não muda depois.
    public async Task<BlocoConteudo> CriarAsync(DadosBloco dados, CancellationToken ct)
    {
        var existentes = await blocos.ListarAsync(ct);
        var slug = Slug.Gerar(dados.Titulo, "bloco");
        var bloco = new BlocoConteudo
        {
            Titulo = dados.Titulo.Trim(),
            Slug = Slug.Unico(slug, await blocos.SlugsComecadosPorAsync(slug, ct)),
            Conteudo = dados.Conteudo,
            Publico = dados.Publico,
            Ordem = existentes.Count == 0 ? 0 : existentes.Max(b => b.Ordem) + 1,
        };
        blocos.Adicionar(bloco);
        await blocos.GravarAsync(ct);
        return bloco;
    }

    // Publico = false oculta o bloco: fica guardado, mas não aparece no site.
    public async Task<Resultado<BlocoConteudo>> EditarAsync(int id, DadosBloco dados, CancellationToken ct)
    {
        var bloco = await blocos.ObterAsync(id, ct);
        if (bloco is null)
            return NaoEncontrado();

        bloco.Titulo = dados.Titulo.Trim();
        bloco.Conteudo = dados.Conteudo;
        bloco.Publico = dados.Publico;
        await blocos.GravarAsync(ct);
        return bloco;
    }

    public async Task<Erro?> RemoverAsync(int id, CancellationToken ct)
    {
        var bloco = await blocos.ObterAsync(id, ct);
        if (bloco is null)
            return NaoEncontrado();

        blocos.Remover(bloco);
        await blocos.GravarAsync(ct);
        return null;
    }

    // Grava a ordem depois de arrastar. A lista tem de ter todos os blocos, cada um uma vez,
    // para nunca ficarem dois blocos com a mesma posição por engano.
    public async Task<Erro?> ReordenarAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        var todos = await blocos.ListarAsync(ct);
        var posicoes = ids.Distinct().Select((id, posicao) => (id, posicao)).ToDictionary(p => p.id, p => p.posicao);
        var completa = ids.Count == todos.Count && posicoes.Count == ids.Count && todos.All(b => posicoes.ContainsKey(b.Id));
        if (!completa)
            return Erro.Invalido("ids", "A lista tem de ter todos os blocos, cada um uma vez.");

        foreach (var bloco in todos)
            bloco.Ordem = posicoes[bloco.Id];
        await blocos.GravarAsync(ct);
        return null;
    }

    private static Erro NaoEncontrado() => Erro.NaoEncontrado("Bloco não encontrado.");
}
