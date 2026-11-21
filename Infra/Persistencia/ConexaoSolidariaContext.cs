using Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistencia;

public sealed class ConexaoSolidariaContext(DbContextOptions<ConexaoSolidariaContext> options)
    : DbContext(options)
{
    public DbSet<Campanha> Campanhas => Set<Campanha>();

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
    }
}
