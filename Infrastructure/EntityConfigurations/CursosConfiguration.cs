using Domain.Alumnos;
using Domain.Cursos;
using Domain.Cursos.Divisiones.Cursantes;
using Domain.Docentes;
using Domain.Materias;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class CursosConfiguration : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> builder)
    {
        ConfigureTablaCursos(builder);
        ConfigureTablaDivisiones(builder);
    }

    private void ConfigureTablaCursos(EntityTypeBuilder<Curso> builder)
    {
        builder.ToTable("curso");

        // PK_CURSOS
        builder.HasKey(x => x.Id)
               .HasName("PK_CURSO");

        builder.Property(x => x.Id)
               .HasColumnName("curso_id")
               .ValueGeneratedNever();

        builder.HasMany<Materia>()
               .WithOne()
               .HasPrincipalKey(x => x.Id)
               .HasForeignKey(x => x.CursoID);

        builder.Property(x => x.Grado)
               .HasColumnName("grado")
               .HasColumnType("varchar(10)")
               .HasConversion(toProvider => toProvider.ToString(),
                              fromProvider => (Grado) Enum.Parse(typeof(Grado), fromProvider));

        builder.Property(x => x.NivelEducativo)
               .HasColumnName("nivel_educativo")
               .HasColumnType("varchar(15)")
               .HasConversion(toProvider => toProvider.ToString(),
                              fromProvider => (NivelEducativo) Enum.Parse(typeof(NivelEducativo), fromProvider));

        /*builder.HasMany(x => x.Divisiones)
               .WithOne()
               .HasForeignKey(x => x.CursoID)
               .HasPrincipalKey(x => x.Id)
               .HasConstraintName("FK_CURSO_DIVISION");*/

        builder.Ignore(x => x.CantidadDivisiones);
        builder.Ignore(x => x.CantidadAlumnos);

        builder.Metadata.FindNavigation(nameof(Curso.Divisiones))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);
    }

    private void ConfigureTablaDivisiones(EntityTypeBuilder<Curso> builder)
    {
        builder.OwnsMany(x => x.Divisiones, divisionBuilder =>
        {
            divisionBuilder.ToTable("division");

            // FK_CURSO_DIVISION
            divisionBuilder.WithOwner()
                           .HasForeignKey("curso_id")
                           .HasConstraintName("FK_CURSO_DIVISION");

            divisionBuilder.HasKey("curso_id", "Id")
                           .HasName("PK_DIVISION");

            divisionBuilder.Property(x => x.Id)
                           .HasColumnName("division_id")
                           .ValueGeneratedNever();

            divisionBuilder.Property(x => x.Descripcion)
                           .HasColumnName("division")
                           .HasColumnType("char(1)")
                           .IsRequired();

            divisionBuilder.Property(x => x.Preceptor)
                           .HasColumnName("docente_id");

            // FK_PRECEPTOR_DIVISION
            divisionBuilder.HasOne<Docente>()
                           .WithMany()
                           .HasForeignKey(x => x.Preceptor)
                           .HasConstraintName("FK_PRECEPTOR_DIVISION");

            divisionBuilder.OwnsMany(x => x.Cursantes, cursanteBuilder =>
            {
                cursanteBuilder.ToTable("cursante");

                // FK_DIVISION_CURSANTE
                cursanteBuilder.WithOwner()
                               .HasForeignKey("curso_id", "division_id")
                               .HasConstraintName("FK_DIVISION_CURSANTE");

                // PK_CURSANTE
                cursanteBuilder.HasKey("curso_id", "division_id", "Id")
                               .HasName("PK_CURSANTE");

                /*// SHADOW PROPERTY
                cursanteBuilder.Property<int>("cursante_id")
                               .UseIdentityColumn();*/

                cursanteBuilder.Property(x => x.Id)
                               .HasColumnName("cursante_id")
                               .ValueGeneratedNever();

                cursanteBuilder.Property(x => x.AlumnoID)
                               .HasColumnName("alumno_id");

                // FK_ALUMNO_CURSANTE
                cursanteBuilder.HasOne<Alumno>()
                               .WithOne()
                               .HasForeignKey<Cursante>(x => x.AlumnoID)
                               .HasConstraintName("FK_ALUMNO_CURSANTE");

                /*// FK_CICLO-LECTIVO_CURSANTES
                cursanteBuilder.HasOne(x => x.CicloLectivo)
                               .WithMany()
                               .HasForeignKey("ciclo_lectivo_id")
                               .HasConstraintName("FK_CICLO-LECTIVO_CURSANTE");*/

                #region Ciclo Lectivo
                cursanteBuilder.HasOne<CicloLectivo>(x => x.CicloLectivo)
                               .WithMany()
                               .HasForeignKey("ciclo_lectivo_id")
                               .HasConstraintName("FK_CICLO-LECTIVO_CURSANTE");
                /*cursanteBuilder.OwnsOne(x => x.CicloLectivo, cicloLectivoBuilder =>
                {
                    cicloLectivoBuilder.ToTable("ciclo_lectivo");

                    cicloLectivoBuilder.WithOwner()
                                       .HasForeignKey("curso_id", "division_id", "cursante_id");
                    
                    cicloLectivoBuilder.Property(x => x.Periodo)
                                       .HasColumnName("periodo")
                                       .HasMaxLength(4);
                });*/
                #endregion

                #region Calificación
                cursanteBuilder.OwnsMany(x => x.Calificaciones, calificacionBuilder =>
                {
                    calificacionBuilder.ToTable("calificacion");

                    // FK_CURSANTES_CALIFICACIONES
                    calificacionBuilder.WithOwner()
                                       .HasForeignKey("curso_id", "division_id", "cursante_id")
                                       .HasConstraintName("FK_CURSANTE_CALIFICACION");

                    // SHADOW PROPERTY
                    calificacionBuilder.Property<int>("calificacion_id")
                                       .UseIdentityColumn();

                    // FK_MATERIAS_CALIFICACIONES
                    calificacionBuilder.HasOne<Materia>()
                                       .WithMany()
                                       .HasForeignKey(x => new { x.CursoID, x.MateriaID })
                                       .HasConstraintName("FK_MATERIA_CALIFICACION")
                                       .OnDelete(DeleteBehavior.NoAction);

                    calificacionBuilder.Property(x => x.CursoID)
                                       .HasColumnName("materia_curso_id");

                    calificacionBuilder.Property(x => x.MateriaID)
                                       .HasColumnName("materia_materia_id");

                    // PK_CALIFICACIONES
                    calificacionBuilder.HasKey("curso_id", "division_id", "cursante_id", "calificacion_id");

                    calificacionBuilder.Property(x => x.Fecha)
                                       .HasColumnName("fecha")
                                       .HasColumnType("date");

                    calificacionBuilder.Property("_asistencia")
                                       .HasColumnName("asistencia")
                                       .HasColumnType("bit");

                    calificacionBuilder.Property(x => x.Instancia)
                                       .HasColumnName("instancia")
                                       .HasColumnType("varchar(15)")
                                       .HasConversion(toProvider => toProvider.ToString(),
                                                      fromProvider => (Instancia) Enum.Parse(typeof(Instancia), fromProvider));

                    calificacionBuilder.Property(x => x.Nota)
                                       .HasColumnName("nota")
                                       .HasColumnType("decimal(5,2)")
                                       .IsRequired(false);
                })
                    .Navigation(x => x.Calificaciones)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
                #endregion
            })
                .Navigation(x => x.Cursantes)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}