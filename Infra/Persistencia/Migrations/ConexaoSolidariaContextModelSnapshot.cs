using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infra.Persistencia.Migrations;

[DbContext(typeof(ConexaoSolidariaContext))]
internal sealed class ConexaoSolidariaContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("Domain.Entidades.Campanha", campanha =>
        {
            campanha.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            campanha.Property<DateTimeOffset>("DataFim").HasColumnType("timestamp with time zone");
            campanha.Property<DateTimeOffset>("DataInicio").HasColumnType("timestamp with time zone");
            campanha.Property<string>("Descricao").IsRequired().HasColumnType("text");
            campanha.Property<decimal>("MetaFinanceira").HasPrecision(31, 2).HasColumnType("numeric(31,2)");
            campanha.Property<int>("Status").HasColumnType("integer");
            campanha.Property<string>("Titulo").IsRequired().HasColumnType("text");
            campanha.Property<uint>("xmin").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("xid");
            campanha.HasKey("Id");
            campanha.ToTable("Campanhas");
        });
    }
}
