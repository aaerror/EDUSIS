using Domain.Cursos;
using Domain.Cursos.Divisiones;
using Domain.Docentes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class DivisionesConfigurations : IEntityTypeConfiguration<Division>
{
    public void Configure(EntityTypeBuilder<Division> builder)
    {
        builder.ToTable("divisiones");

        // FK_CURSOS_DIVISIONES
        builder.HasOne<Curso>()
               .WithMany(x => x.Divisiones)
               .HasForeignKey("curso_id")
               .HasConstraintName("FK_CURSOS_DIVISIONES");

        builder.Property(x => x.Descripcion)
               .HasColumnName("descripicion")
               .HasColumnType("char(1)")
               .IsRequired();

        // PK_DIVISIONES
        builder.HasKey("curso_id", "Id")
               .HasName("PK_DIVISIONES");

        builder.Property(x => x.Id)
               .HasColumnName("division_id")
               .ValueGeneratedNever();

        // FK_PRECEPTORES_DIVISIONES
        builder.HasOne<Preceptor>()
               .WithOne()
               .HasForeignKey<Division>(x => x.Preceptor)
               .HasConstraintName("FK_PRECEPTORES_DIVISIONES");

        builder.Property(x => x.Preceptor)
               .HasColumnName("preceptor_id");

/*        builder.Metadata.FindNavigation(nameof(Division.ListadosDefinitivos))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);*/

        /*
                 builder.OwnsMany(x => x.ListadosDefinitivos, cursantesBuilder =>
                 {
                     cursantesBuilder.ToTable("cursantes");

                     cursantesBuilder.WithOwner()
                                     .HasForeignKey("division_id")
                                     .HasConstraintName("FK_DIVISIONES_CURSANTES");

                     cursantesBuilder.Property<int>("cursante_id")
                                     .UseIdentityColumn();

                     cursantesBuilder.HasKey("division_id", "cursante_id")
                                     .HasName("PK_CURSANTES");

                     cursantesBuilder.Property(x => x.Alumno)
                                     .HasColumnName("alumno_id");

                     cursantesBuilder.HasOne<Alumno>()
                                     .WithOne()
                                     .HasForeignKey<Cursante>(x => x.Alumno)
                                     .HasConstraintName("FK_ALUMNOS_CURSANTES");

                     cursantesBuilder.HasOne(x => x.CicloLectivo)
                                     .WithMany()
                                     .HasForeignKey("ciclo_lectivo_id")
                                     .HasConstraintName("FK_CICLO-LECTIVO_CURSANTES");
                 });
        */
    }
}
