using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Core.Entidades;

namespace Portfolio.Infrastructure.Dados.Configuracoes;

public class UtilizadorConfiguracao : IEntityTypeConfiguration<Utilizador>
{
    public void Configure(EntityTypeBuilder<Utilizador> e)
    {
        // Só uma conta (RF02): a única linha possível é a de Id 1.
        // O Id não é identity: com uma sequência, um INSERT falhado gastava o 1
        // e a conta nunca mais podia ser criada.
        e.Property(u => u.Id).ValueGeneratedNever();
        e.ToTable(t => t.HasCheckConstraint("CK_Utilizador_Unico", "\"Id\" = 1"));

        e.Property(u => u.Nome).HasMaxLength(150);
        e.Property(u => u.Email).HasColumnType("citext").HasMaxLength(254);
        e.HasIndex(u => u.Email).IsUnique();
        e.Property(u => u.PasswordHash).HasMaxLength(255);
        e.Property(u => u.SecurityStamp).HasMaxLength(64);
        e.Property(u => u.TotpSegredoCifrado).HasMaxLength(500);

        e.HasMany(u => u.CodigosRecuperacao)
            .WithOne()
            .HasForeignKey(c => c.UtilizadorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CodigoRecuperacaoConfiguracao : IEntityTypeConfiguration<CodigoRecuperacao>
{
    public void Configure(EntityTypeBuilder<CodigoRecuperacao> e)
    {
        e.Property(c => c.CodigoHash).HasMaxLength(255);
    }
}
