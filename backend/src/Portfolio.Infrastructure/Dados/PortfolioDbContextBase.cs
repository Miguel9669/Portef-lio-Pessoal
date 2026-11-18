using Microsoft.EntityFrameworkCore;
using Portfolio.Core.Entidades;

namespace Portfolio.Infrastructure.Dados;

// Modelo comum aos dois DbContexts (SEG02). As tabelas só estão definidas aqui.
public abstract class PortfolioDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<Utilizador> Utilizadores => Set<Utilizador>();
    public DbSet<CodigoRecuperacao> CodigosRecuperacao => Set<CodigoRecuperacao>();
    public DbSet<BlocoConteudo> BlocosConteudo => Set<BlocoConteudo>();
    public DbSet<Projeto> Projetos => Set<Projeto>();
    public DbSet<Nota> Notas => Set<Nota>();
    public DbSet<ImagemProjeto> ImagensProjeto => Set<ImagemProjeto>();
    public DbSet<ImagemNota> ImagensNota => Set<ImagemNota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Email sem distinguir maiúsculas.
        modelBuilder.HasPostgresExtension("citext");

        // Chaves primárias GENERATED ALWAYS AS IDENTITY.
        modelBuilder.UseIdentityAlwaysColumns();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContextBase).Assembly);

        // Tabelas com o nome da entidade em C# (Projeto, não Projetos).
        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
            entidade.SetTableName(entidade.ClrType.Name);
    }
}
