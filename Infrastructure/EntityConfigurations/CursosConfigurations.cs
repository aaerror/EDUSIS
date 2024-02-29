using Domain.Cursos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class CursosConfigurations : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> builder)
    {
        ConfigureTablaCursos(builder);
        //ConfigureTablaMaterias(builder);
        //ConfigureTablaDivisiones(builder);
    }

    private void ConfigureTablaCursos(EntityTypeBuilder<Curso> builder)
    {
        builder.ToTable("cursos");

        // PK_CURSOS
        builder.HasKey(x => x.Id)
               .HasName("PK_CURSOS");

        builder.Property(x => x.Id)
               .HasColumnName("curso_id")
               .ValueGeneratedNever();

        builder.Property(x => x.Descripcion)
               .HasColumnName("descripcion")
               .HasColumnType("varchar(15)");

        builder.Property(x => x.NivelEducativo)
               .HasColumnName("nivel_educativo")
               .HasColumnType("varchar(15)")
               .HasConversion(nivelEducativo => nivelEducativo.ToString(), value => (NivelEducativo)Enum.Parse(typeof(NivelEducativo), value));

        builder.Ignore(x => x.CantidadDivisiones);
        builder.Ignore(x => x.CantidadMaterias);
        builder.Ignore(x => x.CantidadAlumnos);

        builder.Metadata.FindNavigation(nameof(Curso.Materias))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Curso.Divisiones))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
/*private void ConfigureTablaMaterias(EntityTypeBuilder<Curso> builder)
{
    builder.OwnsMany(x => x.Materias, materiasBuilder =>
    {
        materiasBuilder.ToTable("materias");

        // FK_CURSOS_MATERIAS
        materiasBuilder.WithOwner()
                        .HasForeignKey("curso_id")
                        .HasConstraintName("FK_CURSOS_MATERIAS");

        materiasBuilder.Property(x => x.Id)
                        .HasColumnName("materia_id")
                        .ValueGeneratedNever();

        // PK_MATERIAS
        materiasBuilder.HasKey("curso_id", "Id")
                        .HasName("PK_MATERIAS");

        materiasBuilder.Property(x => x.Descripcion)
                        .HasColumnName("descripcion")
                        .HasColumnType("varchar(30)")
                        .IsRequired();

        materiasBuilder.Property(x => x.HorasCatedra)
                        .HasColumnName("horas_catedra")
                        .HasColumnType("tinyint")
                        .IsRequired();

        // IGNORE
        materiasBuilder.Ignore(x => x.Profesor);

        // SITUACION REVISTA
        materiasBuilder.OwnsMany(x => x.Profesores, situacionRevistaBuilder =>
        {
            situacionRevistaBuilder.ToTable("situacion_revista");

            // SHADOW PROPERTY
            situacionRevistaBuilder.Property<int>("situacion_revista_id")
                                    .UseIdentityColumn();

            // FK_MATERIAS_SITUACION-REVISTA
            situacionRevistaBuilder.WithOwner()
                                    .HasForeignKey("curso_id", "materia_id")
                                    .HasConstraintName("FK_MATERIAS_SITUACION-REVISTA");

            // PK_SITUACION-REVISTA
            situacionRevistaBuilder.HasKey("curso_id", "materia_id", "situacion_revista_id")
                                    .HasName("PK_SITUACION-REVISTA");

            situacionRevistaBuilder.Property(x => x.ProfesorId)
                                    .HasColumnName("profesor_id");

            // FK_PROFESORES_SITUACION-REVISTA
            situacionRevistaBuilder.HasOne<Docente>()
                                    .WithMany()
                                    .HasForeignKey(x => x.ProfesorId)
                                    .HasConstraintName("FK_DOCENTES_SITUACION-REVISTA");
                                    *//*.WithOne()
                                    .HasForeignKey<SituacionRevista>(x => x.ProfesorId)
                                    .HasConstraintName("FK_DOCENTES_SITUACION-REVISTA");*//*

            situacionRevistaBuilder.Property(x => x.Cargo)
                                    .HasColumnName("cargo")
                                    .HasColumnType("varchar(10)")
                                    .HasConversion(cargo => cargo.ToString(), value => (Cargo) Enum.Parse(typeof(Cargo), value));

            situacionRevistaBuilder.Property(x => x.FechaAlta)
                                    .HasColumnName("fecha_alta")
                                    .HasColumnType("date");

            situacionRevistaBuilder.Property(x => x.FechaBaja)
                                    .HasColumnName("fecha_baja")
                                    .HasColumnType("date")
                                    .IsRequired(false);

            situacionRevistaBuilder.Property(x => x.EnFunciones)
                                    .HasColumnName("en_funciones")
                                    .HasDefaultValue(false);
        });

        // HORARIOS
        materiasBuilder.OwnsMany(m => m.Horarios, horarioBuilder =>
        {
            horarioBuilder.ToTable("horarios");

            // SHADOW PROPERTY
            horarioBuilder.Property<int>("horario_id")
                            .UseIdentityColumn();

            // FK_MATERIAS_HORARIOS
            horarioBuilder.WithOwner()
                            .HasForeignKey("curso_id", "materia_id")
                            .HasConstraintName("FK_MATERIAS_HORARIOS");

            // PK_HORARIOS
            horarioBuilder.HasKey("curso_id", "materia_id", "horario_id")
                            .HasName("PK_HORARIOS");

            horarioBuilder.Property(x => x.DiaSemana)
                            .HasColumnName("dia")
                            .HasColumnType("varchar(10)")
                            .HasConversion(dia => dia.ToString(), value => (Dia)Enum.Parse(typeof(Dia), value));
                
            horarioBuilder.Property(x => x.HoraInicio)
                            .HasColumnName("hora_inicio")
                            .HasColumnType("time(0)")
                            .HasConversion(hora => TimeSpan.Parse(hora.ToString()), value => TimeOnly.FromTimeSpan(value));
                
            horarioBuilder.Property(x => x.HoraFin)
                            .HasColumnName("hora_fin")
                            .HasColumnType("time(0)")
                            .HasConversion(hora => TimeSpan.Parse(hora.ToString()), value => TimeOnly.FromTimeSpan(value));

            horarioBuilder.Property(x => x.Turno)
                            .HasColumnName("turno")
                            .HasColumnType("varchar(10)")
                            .HasConversion(turno => turno.ToString(), value => (Turno) Enum.Parse(typeof(Turno), value));
        });
    });
}*/
 /*
    private void ConfigureTablaDivisiones(EntityTypeBuilder<Curso> builder)
    {
        builder.OwnsMany(x => x.Divisiones, divisionBuilder =>
        {
            divisionBuilder.ToTable("divisiones");

            // FK_CURSOS_DIVISIONES
            divisionBuilder.WithOwner()
                           .HasForeignKey("curso_id")
                           .HasConstraintName("FK_CURSOS_DIVISIONES");

            // PK_DIVISIONES
            divisionBuilder.HasKey("curso_id", "Id")
                           .HasName("PK_DIVISIONES");

            divisionBuilder.Property(x => x.Id)
                           .HasColumnName("division_id")
                           .ValueGeneratedNever();

            divisionBuilder.Property(x => x.Descripcion)
                           .HasColumnName("division")
                           .HasColumnType("char(1)")
                           .IsRequired();

            divisionBuilder.Property(x => x.Preceptor)
                           .HasColumnName("preceptor_id");

            // FK_PRECEPTORES_DIVISIONES
            divisionBuilder.HasOne<Preceptor>()
                           .WithMany()
                           .HasForeignKey(x => x.Preceptor)
                           .HasConstraintName("FK_PRECEPTORES_DIVISIONES");

            divisionBuilder.OwnsMany(x => x.ListadosDefinitivos, cursantesBuilder =>
            {
                cursantesBuilder.ToTable("cursantes");

                // SHADOW PROPERTY
                cursantesBuilder.Property<int>("cursante_id")
                                .UseIdentityColumn();

                // FK_DIVISIONES_CURSANTES
                cursantesBuilder.WithOwner()
                                .HasForeignKey("curso_id", "division_id")
                                .HasConstraintName("FK_DIVISIONES_CURSANTES");

                // PK_CURSANTES
                cursantesBuilder.HasKey("curso_id", "division_id", "cursante_id")
                                .HasName("PK_CURSANTES");

                cursantesBuilder.Property(x => x.Alumno)
                                .HasColumnName("alumno_id");

                // FK_ALUMNOS_CURSANTES
                cursantesBuilder.HasOne<Alumno>()
                                .WithOne()
                                .HasForeignKey<Cursante>(x => x.Alumno)
                                .HasConstraintName("FK_ALUMNOS_CURSANTES");

                // FK_CICLO-LECTIVO_CURSANTES
                cursantesBuilder.HasOne(x => x.CicloLectivo)
                                .WithMany()
                                .HasForeignKey("ciclo_lectivo_id")
                                .HasConstraintName("FK_CICLO-LECTIVO_CURSANTES");
            });

            builder.HasMany<Alumno>()
                   .WithMany()
                   .UsingEntity(x =>
                   {
                       x.ToTable("alumnos_divisiones");

                       x.Property("AlumnoId")
                        .HasColumnName("alumno_id");

                       x.Property("DivisionId")
                        .HasColumnName("division_id");

                       x.Property("Divisioncurso_id")
                        .HasColumnName("curso_id");
                   });

        });
    }
*/
