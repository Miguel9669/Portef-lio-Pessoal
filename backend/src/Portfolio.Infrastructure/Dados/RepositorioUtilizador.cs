using Microsoft.EntityFrameworkCore;
using Portfolio.Core.Autenticacao;
using Portfolio.Core.Entidades;

namespace Portfolio.Infrastructure.Dados;

// O utilizador do backoffice, pelo BackofficeDbContext (SEG02). Só existe um (RF02).
public sealed class RepositorioUtilizador(BackofficeDbContext bd) : IRepositorioUtilizador
{
    // O email é citext, por isso a comparação não distingue maiúsculas.
    public Task<Utilizador?> ObterPorEmailAsync(string email, CancellationToken ct) =>
        bd.Utilizadores.SingleOrDefaultAsync(u => u.Email == email, ct);

    public Task<Utilizador?> ObterAsync(CancellationToken ct) =>
        bd.Utilizadores.Include(u => u.CodigosRecuperacao).SingleOrDefaultAsync(ct);

    public Task<bool> ExisteAsync(CancellationToken ct) => bd.Utilizadores.AnyAsync(ct);

    public Task<string?> ObterSecurityStampAsync(CancellationToken ct) =>
        bd.Utilizadores.Select(u => u.SecurityStamp).SingleOrDefaultAsync(ct);

    public void Adicionar(Utilizador utilizador) => bd.Utilizadores.Add(utilizador);

    public Task GravarAsync(CancellationToken ct) => bd.SaveChangesAsync(ct);
}
