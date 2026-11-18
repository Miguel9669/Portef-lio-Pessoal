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
        bd.Projetos.OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id).ToListAsync(ct);

    public Task<Projeto?> ObterAsync(int id, CancellationToken ct) =>
        bd.Projetos.SingleOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<string>> SlugsComecadosPorAsync(string inicio, CancellationToken ct) =>
        bd.Projetos.Where(p => p.Slug.StartsWith(inicio)).Select(p => p.Slug).ToListAsync(ct);

    public void Adicionar(Projeto projeto) => bd.Projetos.Add(projeto);

    public void Remover(Projeto projeto) => bd.Projetos.Remove(projeto);

    public Task GravarAsync(CancellationToken ct) => bd.SaveChangesAsync(ct);
}
