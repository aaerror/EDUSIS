using Domain.Alumnos;
using Domain.Cursos.Divisiones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class CursantesConfigurations : IEntityTypeConfiguration<Cursante>
{
    public void Configure(EntityTypeBuilder<Cursante> builder)
    {
        builder.ToTable("cursantes");

        builder.Property<int>("cursante_id")
               .UseIdentityColumn();

        // PK_CURSANTES
        builder.HasKey("cursante_id")
               .HasName("PK_CURSANTES");

        // FK_DIVISIONES_CURSANTES
        builder.HasOne<Division>()
               .WithMany("_listadosDefinitivos")
               .HasForeignKey("curso_id", "division_id")
               .HasConstraintName("FK_DIVISIONES_CURSANTES");

        // FK_ALUMNOS_CURSANTES
        builder.HasOne<Alumno>()
               .WithOne()
               .HasForeignKey<Cursante>(x => x.Alumno)
               .HasConstraintName("FK_ALUMNOS_CURSANTES");

        builder.Property(x => x.Alumno)
               .HasColumnName("alumno_id");

        // FK_CICLO-LECTIVO_CURSANTES
        builder.HasOne(x => x.CicloLectivo)
               .WithMany()
               .HasForeignKey("ciclo_lectivo_id")
               .HasConstraintName("FK_CICLO-LECTIVO_CURSANTES");
    }
}