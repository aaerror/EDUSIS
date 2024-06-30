using Domain.Cursos.Divisiones.Cursantes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class CicloLectivoConfiguration : IEntityTypeConfiguration<CicloLectivo>
{
    public void Configure(EntityTypeBuilder<CicloLectivo> builder)
    {
        builder.ToTable("ciclo_lectivo");

        builder.Property<int>("ciclo_lectivo_id");

        builder.HasKey("ciclo_lectivo_id")
               .HasName("PK_CICLO-LECTIVO");

        builder.Property(x => x.Periodo)
               .HasColumnName("periodo")
               .HasColumnType("smallint")
               .HasMaxLength(4)
               .HasConversion(
                    toProvider => Int32.Parse(toProvider),
                    fromProvider => fromProvider.ToString());
    }
}
