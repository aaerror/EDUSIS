using Domain.Alumnos;
using Domain.Cursos;
using Domain.Cursos.Divisiones;
using Domain.Cursos.Divisiones.Cursantes;
using Domain.Cursos.Materias;
using Domain.Docentes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class DivisionesConfigurations : IEntityTypeConfiguration<Division>
{
    public void Configure(EntityTypeBuilder<Division> builder)
    {
        builder.ToTable("divisiones");

        // PK_DIVISIONES
        builder.HasKey(x => x.Id)
               .HasName("PK_DIVISIONES");

        builder.Property(x => x.Id)
               .HasColumnName("division_id")
               .ValueGeneratedNever();

        // FK_CURSOS_DIVISIONES
        builder.HasOne<Curso>()
               .WithMany(x => x.Divisiones)
               .HasForeignKey(x => x.CursoID)
               .HasConstraintName("FK_CURSOS_DIVISIONES");

        builder.Property(x => x.CursoID)
               .HasColumnName("curso_id")
               .HasColumnOrder(0);

        builder.Property(x => x.Descripcion)
               .HasColumnName("descripicion")
               .HasColumnType("varchar(15)")
               .IsRequired();

        // FK_PRECEPTORES_DIVISIONES
        builder.HasOne<Docente>()
               .WithOne()
               .HasForeignKey<Division>(x => x.Preceptor)
               .HasConstraintName("FK_DOCENTES_DIVISIONES")
               .IsRequired(false);

        builder.Property(x => x.Preceptor)
               .HasColumnName("preceptor_id");

        // CURSANTES
        builder.OwnsMany(x => x.ListadosDefinitivos, cursantesBuilder =>
        {
            cursantesBuilder.ToTable("cursantes");

            // PK_CURSANTES
            cursantesBuilder.HasKey(x => x.Id)
                            .HasName("PK_CURSANTES");

            cursantesBuilder.Property(x => x.Id)
                            .HasColumnName("cursante_id")
                            .ValueGeneratedNever();

            // FK_DIVISIONES_CURSANTES
            cursantesBuilder.WithOwner()
                            .HasForeignKey(x => x.DivisionID)
                            .HasConstraintName("FK_DIVISIONES_CURSANTES");

            cursantesBuilder.Property(x => x.DivisionID)
                            .HasColumnName("division_id")
                            .HasColumnOrder(0);

            // FK_ALUMNOS_CURSANTES
            cursantesBuilder.HasOne<Alumno>()
                            .WithMany()
                            .HasForeignKey(x => x.AlumnoID)
                            .HasConstraintName("FK_ALUMNOS_CURSANTES");

            cursantesBuilder.Property(x => x.AlumnoID)
                            .HasColumnName("alumno_id");

            // CICLO_LECTIVO
            cursantesBuilder.OwnsOne(x => x.CicloLectivo, cicloLectivoBuilder =>
            {
/*
                cicloLectivoBuilder.ToTable("ciclo_lectivo");

                cicloLectivoBuilder.WithOwner()
                                   .HasForeignKey("curso_id", "division_id", "cursante_id");
*/

                cicloLectivoBuilder.Property(x => x.Periodo)
                                   .HasColumnName("periodo")
                                   .HasMaxLength(4);

            });

            cursantesBuilder.OwnsMany(x => x.Calificaciones, calificacionBuilder =>
            {
                calificacionBuilder.ToTable("calificaciones");

                // SHADOW PROPERTY
                calificacionBuilder.Property<int>("calificacion_id")
                                   .UseIdentityColumn();

                // PK_CALIFICACIONES
                calificacionBuilder.HasKey("calificacion_id");

                // FK_CURSANTES_CALIFICACIONES
                calificacionBuilder.WithOwner()
                                   .HasForeignKey("cursante_id")
                                   .HasConstraintName("FK_CURSANTES_CALIFICACIONES");


                // FK_MATERIAS_CALIFICACIONES
                calificacionBuilder.HasOne<Materia>()
                                   .WithMany()
                                   .HasForeignKey(x => x.MateriaID)
                                   .HasConstraintName("FK_MATERIAS_CALIFICACIONES")
                                   .OnDelete(DeleteBehavior.NoAction);

                calificacionBuilder.Property(x => x.MateriaID)
                                   .HasColumnName("materia_id")
                                   .IsRequired(true);

                calificacionBuilder.Property(x => x.Fecha)
                                   .HasColumnName("fecha")
                                   .HasColumnType("date");

                calificacionBuilder.Property("_asistencia")
                                   .HasColumnName("asistencia")
                                   .HasColumnType("bit");

                calificacionBuilder.Property(x => x.Instancia)
                                   .HasColumnName("instancia")
                                   .HasColumnType("varchar(15)")
                                   .HasConversion(instancia => instancia.ToString(),
                                                  value => (Instancia) Enum.Parse(typeof(Instancia), value));

                calificacionBuilder.Property(x => x.Nota)
                                   .HasColumnName("nota")
                                   .HasColumnType("decimal(5,2)")
                                   .IsRequired(false);
            });

            // Calificaciones
            /*cursantesBuilder.Metadata.FindNavigation(nameof(Cursante.Calificaciones))
                            .SetPropertyAccessMode(PropertyAccessMode.Field);*/

            /*cursantesBuilder.HasOne(x => x.CicloLectivo)
                              .WithMany()
                              .HasForeignKey("ciclo_lectivo_id")
                              .HasConstraintName("FK_CICLO-LECTIVO_CURSANTES");*/
        });

        builder.Metadata.FindNavigation(nameof(Division.ListadosDefinitivos))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
