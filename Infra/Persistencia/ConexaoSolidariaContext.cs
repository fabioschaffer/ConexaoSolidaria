using Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistencia;

public sealed class ConexaoSolidariaContext(DbContextOptions<ConexaoSolidariaContext> options)
    : DbContext(options)
{
    public DbSet<Campanha> Campanhas => Set<Campanha>();
    public DbSet<Doacao> Doacoes => Set<Doacao>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var campanha = modelBuilder.Entity<Campanha>();
        campanha.ToTable("Campanhas");
        campanha.HasKey(c => c.Id);
        campanha.Property(c => c.Id).UseIdentityByDefaultColumn();
        campanha.Property(c => c.Titulo).IsRequired();
        campanha.Property(c => c.Descricao).IsRequired();
        campanha.Property(c => c.DataInicio).HasColumnType("timestamp with time zone");
        campanha.Property(c => c.DataFim).HasColumnType("timestamp with time zone");
        campanha.Property(c => c.MetaFinanceira).HasPrecision(31, 2);
        campanha.Property(c => c.ValorTotalArrecadado).HasPrecision(31, 2);
        campanha.Property<uint>("xmin").IsRowVersion();

        var usuario = modelBuilder.Entity<Usuario>();
        usuario.ToTable("Usuarios");
        usuario.HasKey(u => u.Id);
        usuario.Property(u => u.Id).UseIdentityByDefaultColumn();
        usuario.Property(u => u.Nome).IsRequired();
        usuario.Property(u => u.Email).IsRequired();
        usuario.Property(u => u.Cpf).HasMaxLength(11).IsRequired();
        usuario.Property(u => u.Senha).IsRequired();
        usuario.Property(u => u.Perfil).IsRequired();
        usuario.HasIndex(u => u.Email).IsUnique();
        usuario.HasIndex(u => u.Cpf).IsUnique();
        usuario.Property<uint>("xmin").IsRowVersion();

        var doacao = modelBuilder.Entity<Doacao>();
        doacao.ToTable("Doacoes");
        doacao.HasKey(d => d.Id);
        doacao.Property(d => d.Id).UseIdentityByDefaultColumn();
        doacao.Property(d => d.Valor).HasPrecision(31, 2);
        doacao.Property(d => d.Status).IsRequired();
        doacao.HasOne(d => d.Campanha)
            .WithMany()
            .HasForeignKey(d => d.CampanhaId)
            .OnDelete(DeleteBehavior.Restrict);
        doacao.HasOne(d => d.Doador)
            .WithMany()
            .HasForeignKey(d => d.DoadorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
