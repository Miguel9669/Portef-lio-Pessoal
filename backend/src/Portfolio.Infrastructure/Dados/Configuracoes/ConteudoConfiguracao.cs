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

        // Apagar um projeto apaga as notas e as imagens (RF07).
        e.HasMany(p => p.Notas)
            .WithOne()
            .HasForeignKey(n => n.ProjetoId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(p => p.Imagens)
            .WithOne()
            .HasForeignKey(i => i.ProjetoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotaConfiguracao : IEntityTypeConfiguration<Nota>
{
    public void Configure(EntityTypeBuilder<Nota> e)
    {
        e.Property(n => n.Titulo).HasMaxLength(200);
        e.Property(n => n.Texto).HasColumnName("Nota");

        // A consulta da timeline (RF11).
        e.HasIndex(n => new { n.ProjetoId, n.CriadoEm });

        e.HasMany(n => n.Imagens)
            .WithOne()
            .HasForeignKey(i => i.NotaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ImagemProjetoConfiguracao : IEntityTypeConfiguration<ImagemProjeto>
{
    public void Configure(EntityTypeBuilder<ImagemProjeto> e) => ImagemConfiguracao.Configurar(e);
}

public class ImagemNotaConfiguracao : IEntityTypeConfiguration<ImagemNota>
{
    public void Configure(EntityTypeBuilder<ImagemNota> e) => ImagemConfiguracao.Configurar(e);
}

internal static class ImagemConfiguracao
{
    // Mesma estrutura nas duas tabelas de imagens.
    public static void Configurar<T>(EntityTypeBuilder<T> e) where T : Imagem
    {
        e.Property(i => i.Ficheiro).HasMaxLength(100);
        e.HasIndex(i => i.Ficheiro).IsUnique();
        e.Property(i => i.NomeOriginal).HasMaxLength(255);
        e.Property(i => i.TipoMime).HasMaxLength(50);
    }
}
