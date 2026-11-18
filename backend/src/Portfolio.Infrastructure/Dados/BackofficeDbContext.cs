using Microsoft.EntityFrameworkCore;
using Portfolio.Core.Entidades;

namespace Portfolio.Infrastructure.Dados;

// Leitura e escrita, com o utilizador portfolio_escrita (SEG01). Usado pelo backoffice.
// As migrações são geradas a partir deste contexto.
public class BackofficeDbContext(DbContextOptions<BackofficeDbContext> options)
    : PortfolioDbContextBase(options)
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PreencherDatas();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PreencherDatas();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PreencherDatas()
    {
        var agora = DateTime.UtcNow;

        foreach (var entrada in ChangeTracker.Entries<ITemCriadoEm>())
        {
            if (entrada.State == EntityState.Added)
                entrada.Entity.CriadoEm = agora;
        }

        foreach (var entrada in ChangeTracker.Entries<ITemDatas>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
                entrada.Entity.AtualizadoEm = agora;
        }
    }
}
