using Microsoft.EntityFrameworkCore;
using Portfolio.Core.Conteudo;
using Portfolio.Core.Entidades;
using Portfolio.Core.Projetos;

namespace Portfolio.Infrastructure.Dados;

// Blocos e projetos pelo BackofficeDbContext (SEG02).
public sealed class RepositorioBlocos(BackofficeDbContext bd) : IRepositorioBlocos
{
    public Task<List<BlocoConteudo>> ListarAsync(CancellationToken ct) =>
        bd.BlocosConteudo.OrderBy(b => b.Ordem).ThenBy(b => b.Id).ToListAsync(ct);

    public Task<BlocoConteudo?> ObterAsync(int id, CancellationToken ct) =>
        bd.BlocosConteudo.SingleOrDefaultAsync(b => b.Id == id, ct);

    public Task<List<string>> SlugsComecadosPorAsync(string inicio, CancellationToken ct) =>
        bd.BlocosConteudo.Where(b => b.Slug.StartsWith(inicio)).Select(b => b.Slug).ToListAsync(ct);

    public void Adicionar(BlocoConteudo bloco) => bd.BlocosConteudo.Add(bloco);

    public void Remover(BlocoConteudo bloco) => bd.BlocosConteudo.Remove(bloco);

    public Task GravarAsync(CancellationToken ct) => bd.SaveChangesAsync(ct);
}

public sealed class RepositorioProjetos(BackofficeDbContext bd) : IRepositorioProjetos
{
    public Task<List<Projeto>> ListarAsync(CancellationToken ct) =>
        bd.Projetos.Include(p => p.Imagens.OrderBy(i => i.Ordem))
            .OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id).ToListAsync(ct);

    public Task<Projeto?> ObterAsync(int id, CancellationToken ct) =>
        bd.Projetos.Include(p => p.Imagens.OrderBy(i => i.Ordem)).SingleOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<string>> SlugsComecadosPorAsync(string inicio, CancellationToken ct) =>
        bd.Projetos.Where(p => p.Slug.StartsWith(inicio)).Select(p => p.Slug).ToListAsync(ct);

    public void Adicionar(Projeto projeto) => bd.Projetos.Add(projeto);

    public void Remover(Projeto projeto) => bd.Projetos.Remove(projeto);

    public Task GravarAsync(CancellationToken ct) => bd.SaveChangesAsync(ct);
}

public sealed class RepositorioImagens(BackofficeDbContext bd) : Core.Imagens.IRepositorioImagens
{
    public Task<ImagemProjeto?> ObterAsync(int id, CancellationToken ct) =>
        bd.ImagensProjeto.SingleOrDefaultAsync(i => i.Id == id, ct);

    public Task<List<ImagemProjeto>> ListarDoProjetoAsync(int projetoId, CancellationToken ct) =>
        bd.ImagensProjeto.Where(i => i.ProjetoId == projetoId).OrderBy(i => i.Ordem).ThenBy(i => i.Id).ToListAsync(ct);

    public void Adicionar(ImagemProjeto imagem) => bd.ImagensProjeto.Add(imagem);

    public void Remover(ImagemProjeto imagem) => bd.ImagensProjeto.Remove(imagem);

    public Task GravarAsync(CancellationToken ct) => bd.SaveChangesAsync(ct);
}
