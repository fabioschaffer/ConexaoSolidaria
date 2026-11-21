using Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistencia;

public sealed class ConexaoSolidariaContext(DbContextOptions<ConexaoSolidariaContext> options)
    : DbContext(options)
{
    public DbSet<Campanha> Campanhas => Set<Campanha>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var campanha = modelBuilder.Entity<Campanha>();
        campanha.ToTable("Campanhas");
        campanha.HasKey(c => c.Id);
        campanha.Property(c => c.Id).ValueGeneratedNever();
        campanha.Property(c => c.Titulo).IsRequired();
        campanha.Property(c => c.Descricao).IsRequired();
        campanha.Property(c => c.DataInicio).HasColumnType("timestamp with time zone");
        campanha.Property(c => c.DataFim).HasColumnType("timestamp with time zone");
        campanha.Property(c => c.MetaFinanceira).HasPrecision(31, 2);
        campanha.Property<uint>("xmin").IsRowVersion();

        var usuario = modelBuilder.Entity<Usuario>();
        usuario.ToTable("Usuarios");
        usuario.HasKey(u => u.Id);
        usuario.Property(u => u.Id).ValueGeneratedNever();
        usuario.Property(u => u.NomeCompleto).IsRequired();
        usuario.Property(u => u.Email).IsRequired();
        usuario.Property(u => u.Cpf).HasMaxLength(11).IsRequired();
        usuario.Property(u => u.Senha).IsRequired();
        usuario.Property(u => u.Perfil).IsRequired();
    }
}
