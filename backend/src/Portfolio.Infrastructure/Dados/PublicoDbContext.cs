using Microsoft.EntityFrameworkCore;

namespace Portfolio.Infrastructure.Dados;

// Só leitura, com o utilizador portfolio_leitura (SEG01, SEG02). Usado pelo site público.
// SaveChanges lança uma exceção; ExecuteUpdate/ExecuteDelete não passam por aqui,
// mas são recusados pela própria base de dados, porque o utilizador só tem SELECT.
public class PublicoDbContext : PortfolioDbContextBase
{
    public PublicoDbContext(DbContextOptions<PublicoDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException("O PublicoDbContext é só de leitura.");

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("O PublicoDbContext é só de leitura.");
}
