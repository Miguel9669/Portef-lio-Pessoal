using Portfolio.Core.Comum;
using Portfolio.Core.Entidades;
using Portfolio.Core.Imagens;

namespace Portfolio.Core.Projetos;

public interface IRepositorioProjetos
{
    // Do mais recente para o mais antigo, com as imagens.
    Task<List<Projeto>> ListarAsync(CancellationToken ct);

    // Com as imagens, por ordem.
    Task<Projeto?> ObterAsync(int id, CancellationToken ct);
    Task<List<string>> SlugsComecadosPorAsync(string inicio, CancellationToken ct);
    void Adicionar(Projeto projeto);
    void Remover(Projeto projeto);
    Task GravarAsync(CancellationToken ct);
}

public sealed record DadosProjeto(string Nome, string? Descricao, string? RepositorioGithub);

// Projetos no backoffice (RF06–RF08, RF10).
public sealed class ProjetoService(IRepositorioProjetos projetos, IArmazenamentoImagens armazenamento)
{
    private const string LinkInvalido = "Tem de ser o link de um repositório do GitHub, por exemplo https://github.com/dono/repositorio.";

    public Task<List<Projeto>> ListarAsync(CancellationToken ct) => projetos.ListarAsync(ct);

    public async Task<Resultado<Projeto>> ObterAsync(int id, CancellationToken ct) =>
        await projetos.ObterAsync(id, ct) is { } projeto ? projeto : NaoEncontrado();

    // O slug é gerado aqui e não muda depois: os links já partilhados continuam a funcionar
    // mesmo que o nome do projeto mude.
    public async Task<Resultado<Projeto>> CriarAsync(DadosProjeto dados, CancellationToken ct)
    {
        if (!TentarLerLink(dados.RepositorioGithub, out var link))
            return Erro.Invalido(nameof(DadosProjeto.RepositorioGithub), LinkInvalido);

        var slug = Slug.Gerar(dados.Nome, "projeto");
        var projeto = new Projeto
        {
            Nome = dados.Nome.Trim(),
            Slug = Slug.Unico(slug, await projetos.SlugsComecadosPorAsync(slug, ct)),
            Descricao = TextoOuNulo(dados.Descricao),
            RepositorioGithub = link?.Url,
        };
        projetos.Adicionar(projeto);
        await projetos.GravarAsync(ct);
        return projeto;
    }

    public async Task<Resultado<Projeto>> EditarAsync(int id, DadosProjeto dados, CancellationToken ct)
    {
        var projeto = await projetos.ObterAsync(id, ct);
        if (projeto is null)
            return NaoEncontrado();
        if (!TentarLerLink(dados.RepositorioGithub, out var link))
            return Erro.Invalido(nameof(DadosProjeto.RepositorioGithub), LinkInvalido);

        projeto.Nome = dados.Nome.Trim();
        projeto.Descricao = TextoOuNulo(dados.Descricao);
        projeto.RepositorioGithub = link?.Url;
        await projetos.GravarAsync(ct);
        return projeto;
    }

    // RF08: um projeto despublicado sai do site, mas continua no backoffice.
    public async Task<Resultado<Projeto>> PublicarAsync(int id, bool publico, CancellationToken ct)
    {
        var projeto = await projetos.ObterAsync(id, ct);
        if (projeto is null)
            return NaoEncontrado();

        projeto.Publico = publico;
        await projetos.GravarAsync(ct);
        return projeto;
    }

    // RF07: as linhas das imagens saem em cascata na base de dados; os ficheiros são apagados
    // só depois disso ter sucesso, para nunca ficar uma imagem na base de dados sem ficheiro.
    public async Task<Erro?> RemoverAsync(int id, CancellationToken ct)
    {
        var projeto = await projetos.ObterAsync(id, ct);
        if (projeto is null)
            return NaoEncontrado();

        var ficheiros = projeto.Imagens.Select(i => i.Ficheiro).ToList();
        projetos.Remover(projeto);
        await projetos.GravarAsync(ct);
        ficheiros.ForEach(armazenamento.Apagar);
        return null;
    }

    // Sem link é válido (o projeto fica sem timeline); com link, tem de ser do GitHub.
    private static bool TentarLerLink(string? texto, out LinkGithub? link)
    {
        link = LinkGithub.Ler(texto);
        return link is not null || string.IsNullOrWhiteSpace(texto);
    }

    private static string? TextoOuNulo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static Erro NaoEncontrado() => Erro.NaoEncontrado("Projeto não encontrado.");
}
