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

public sealed record DadosProjeto(
    string Nome,
    string? Descricao,
    string? RepositorioGithub,
    string? LinkOficial = null,
    bool CodigoPrivado = false,
    bool HistoricoPublico = false);

// Projetos no backoffice (RF06–RF08, RF10).
public sealed class ProjetoService(IRepositorioProjetos projetos, IArmazenamentoImagens armazenamento)
{
    private const string LinkInvalido = "Tem de ser o link de um repositório do GitHub, por exemplo https://github.com/dono/repositorio.";
    private const string LinkOficialInvalido = "Tem de ser um endereço https:// completo, por exemplo https://meuapp.pt.";

    public Task<List<Projeto>> ListarAsync(CancellationToken ct) => projetos.ListarAsync(ct);

    public async Task<Resultado<Projeto>> ObterAsync(int id, CancellationToken ct) =>
        await projetos.ObterAsync(id, ct) is { } projeto ? projeto : NaoEncontrado();

    // O slug é gerado aqui e não muda depois: os links já partilhados continuam a funcionar
    // mesmo que o nome do projeto mude.
    public async Task<Resultado<Projeto>> CriarAsync(DadosProjeto dados, CancellationToken ct)
    {
        if (Validar(dados) is { } erro)
            return erro;

        var slug = Slug.Gerar(dados.Nome, "projeto");
        var projeto = new Projeto
        {
            Nome = dados.Nome.Trim(),
            Slug = Slug.Unico(slug, await projetos.SlugsComecadosPorAsync(slug, ct)),
        };
        Aplicar(projeto, dados);
        projetos.Adicionar(projeto);
        await projetos.GravarAsync(ct);
        return projeto;
    }

    public async Task<Resultado<Projeto>> EditarAsync(int id, DadosProjeto dados, CancellationToken ct)
    {
        var projeto = await projetos.ObterAsync(id, ct);
        if (projeto is null)
            return NaoEncontrado();
        if (Validar(dados) is { } erro)
            return erro;

        Aplicar(projeto, dados);
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

    // Os dois links são opcionais; se vierem, têm de ser válidos.
    private static Erro? Validar(DadosProjeto dados)
    {
        if (!string.IsNullOrWhiteSpace(dados.RepositorioGithub) && LinkGithub.Ler(dados.RepositorioGithub) is null)
            return Erro.Invalido(nameof(DadosProjeto.RepositorioGithub), LinkInvalido);
        if (!string.IsNullOrWhiteSpace(dados.LinkOficial) && Projetos.LinkOficial.Ler(dados.LinkOficial) is null)
            return Erro.Invalido(nameof(DadosProjeto.LinkOficial), LinkOficialInvalido);
        return null;
    }

    private static void Aplicar(Projeto projeto, DadosProjeto dados)
    {
        projeto.Nome = dados.Nome.Trim();
        projeto.Descricao = TextoOuNulo(dados.Descricao);
        projeto.RepositorioGithub = LinkGithub.Ler(dados.RepositorioGithub)?.Url;
        projeto.LinkOficial = Projetos.LinkOficial.Ler(dados.LinkOficial);
        projeto.CodigoPrivado = dados.CodigoPrivado;
        projeto.HistoricoPublico = dados.HistoricoPublico;
    }

    private static string? TextoOuNulo(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static Erro NaoEncontrado() => Erro.NaoEncontrado("Projeto não encontrado.");
}
