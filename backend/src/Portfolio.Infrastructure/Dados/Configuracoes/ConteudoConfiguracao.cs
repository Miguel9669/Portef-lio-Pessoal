using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Core.Entidades;

namespace Portfolio.Infrastructure.Dados.Configuracoes;

public class BlocoConteudoConfiguracao : IEntityTypeConfiguration<BlocoConteudo>
{
    public void Configure(EntityTypeBuilder<BlocoConteudo> e)
    {
        e.Property(b => b.Titulo).HasMaxLength(150);
        e.Property(b => b.Slug).HasMaxLength(160);
        e.HasIndex(b => b.Slug).IsUnique();
        e.Property(b => b.Publico).HasDefaultValue(false);

        // A consulta do site: blocos públicos por ordem.
        e.HasIndex(b => new { b.Publico, b.Ordem });
    }
}

public class ProjetoConfiguracao : IEntityTypeConfiguration<Projeto>
{
    public void Configure(EntityTypeBuilder<Projeto> e)
    {
        e.Property(p => p.Nome).HasMaxLength(150);
        e.Property(p => p.Slug).HasMaxLength(160);
        e.HasIndex(p => p.Slug).IsUnique();
        e.Property(p => p.Publico).HasDefaultValue(false);
        e.HasIndex(p => p.Publico);
        e.Property(p => p.RepositorioGithub).HasMaxLength(200);

        // SEG29: só links de repositórios do GitHub, também na base de dados.
        // Os nomes seguem as regras do GitHub (dono até 39 caracteres, repositório até 100).
        e.ToTable(t => t.HasCheckConstraint("CK_Projeto_RepositorioGithub",
            "\"RepositorioGithub\" ~ '^https://github\\.com/[A-Za-z0-9-]{1,39}/[A-Za-z0-9._-]{1,100}$'"));

        // Apagar um projeto apaga as imagens (RF07).
        e.HasMany(p => p.Imagens)
            .WithOne()
            .HasForeignKey(i => i.ProjetoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ImagemProjetoConfiguracao : IEntityTypeConfiguration<ImagemProjeto>
{
    public void Configure(EntityTypeBuilder<ImagemProjeto> e)
    {
        e.Property(i => i.Ficheiro).HasMaxLength(100);
        e.HasIndex(i => i.Ficheiro).IsUnique();
        e.Property(i => i.NomeOriginal).HasMaxLength(255);
        e.Property(i => i.TipoMime).HasMaxLength(50);
    }
}
