using Microsoft.EntityFrameworkCore;
using Portfolio.Core.Entidades;
using Portfolio.Core.SitePublico;

namespace Portfolio.Infrastructure.Dados;

// Site público: PublicoDbContext, com o utilizador só de leitura (SEG01, SEG02).
// O filtro Publico = true está em todas as consultas, aqui e só aqui (SEG16).
public sealed class RepositorioPublico(PublicoDbContext bd) : IRepositorioPublico
{
    public Task<List<BlocoConteudo>> ListarBlocosAsync(CancellationToken ct) =>
        bd.BlocosConteudo.Where(b => b.Publico).OrderBy(b => b.Ordem).ThenBy(b => b.Id).ToListAsync(ct);

    public Task<List<Projeto>> ListarProjetosAsync(CancellationToken ct) =>
        Publicos().OrderByDescending(p => p.CriadoEm).ThenByDescending(p => p.Id).ToListAsync(ct);

    public Task<Projeto?> ObterProjetoAsync(string slug, CancellationToken ct) =>
        Publicos().SingleOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<ImagemProjeto?> ObterImagemAsync(string ficheiro, CancellationToken ct) =>
        bd.ImagensProjeto
            .Where(i => i.Ficheiro == ficheiro && bd.Projetos.Any(p => p.Id == i.ProjetoId && p.Publico))
            .SingleOrDefaultAsync(ct);

    private IQueryable<Projeto> Publicos() =>
        bd.Projetos.Where(p => p.Publico).Include(p => p.Imagens.OrderBy(i => i.Ordem));
}
