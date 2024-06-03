using Domain.Cursos;
using Domain.Docentes;
using Domain.Materias;
using Domain.Materias.Horarios;
using Domain.Materias.SituacionRevistaDocente;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class MateriasConfiguration : IEntityTypeConfiguration<Materia>
{
    public void Configure(EntityTypeBuilder<Materia> builder)
    {
        builder.ToTable("materia");

        // PK_MATERIAS
        builder.HasKey(x => x.Id)
               .HasName("PK_MATERIA");

        builder.Property(x => x.Id)
               .HasColumnName("materia_id")
               .ValueGeneratedNever();

        // FK_CURSOS_MATERIAS
        builder.HasOne<Curso>()
               .WithMany(x => x.Materias)
               .HasPrincipalKey(x => x.Id)
               .HasForeignKey(x => x.CursoID)
               .HasConstraintName("FK_CURSO_MATERIA");

        builder.Property(x => x.CursoID)
               .HasColumnName("curso_id")
               .HasColumnOrder(0);

        builder.Property(x => x.Descripcion)
               .HasColumnName("descripcion")
               .HasColumnType("varchar(50)")
               .IsRequired();

        builder.Property(x => x.HorasCatedra)
               .HasColumnName("horas_catedra")
               .HasColumnType("tinyint")
               .IsRequired();

        // IGNORE
        builder.Ignore(x => x.ProfesorID);

        // SITUACION REVISTA
        builder.OwnsMany(x => x.Profesores, situacionRevistaBuilder =>
        {
            situacionRevistaBuilder.ToTable("situacion_revista");

            // SHADOW PROPERTY
            situacionRevistaBuilder.Property<int>("situacion_revista_id")
                                   .UseIdentityColumn();

            // FK_MATERIAS_SITUACION-REVISTA
            situacionRevistaBuilder.WithOwner()
                                   .HasForeignKey("materia_id")
                                   .HasConstraintName("FK_MATERIA_SITUACION-REVISTA");

            // PK_SITUACION-REVISTA
            situacionRevistaBuilder.HasKey("materia_id", "situacion_revista_id")
                                   .HasName("PK_SITUACION-REVISTA");


            situacionRevistaBuilder.Property(x => x.ProfesorID)
                                   .HasColumnName("profesor_id");

            // FK_PROFESORES_SITUACION-REVISTA
            situacionRevistaBuilder.HasOne<Docente>()
                                   .WithMany()
                                   .HasForeignKey(x => x.ProfesorID)
                                   .HasConstraintName("FK_DOCENTE_SITUACION-REVISTA");
            /*.WithOne()
            .HasForeignKey<SituacionRevista>(x => x.ProfesorId)
            .HasConstraintName("FK_DOCENTES_SITUACION-REVISTA");*/

            situacionRevistaBuilder.Property(x => x.Cargo)
                                   .HasColumnName("cargo")
                                   .HasColumnType("varchar(10)")
                                   .HasConversion(toProvider => toProvider.ToString(),
                                                  fromProvider => (Cargo)Enum.Parse(typeof(Cargo), fromProvider));

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
        builder.OwnsMany(m => m.Horarios, horarioBuilder =>
        {
            horarioBuilder.ToTable("horario");

            // SHADOW PROPERTY
            horarioBuilder.Property<int>("horario_id")
                          .UseIdentityColumn();

            // FK_MATERIAS_HORARIOS
            horarioBuilder.WithOwner()
                          .HasForeignKey("materia_id")
                          .HasConstraintName("FK_MATERIA_HORARIO");

            // PK_HORARIOS
            horarioBuilder.HasKey("materia_id", "horario_id")
                          .HasName("PK_HORARIO");

            horarioBuilder.Property(x => x.DiaSemana)
                          .HasColumnName("dia")
                          .HasColumnType("varchar(10)")
                          .HasConversion(toProvider => toProvider.ToString(),
                                         fromProvider => (Dia) Enum.Parse(typeof(Dia), fromProvider));

            horarioBuilder.Property(x => x.HoraInicio)
                          .HasColumnName("hora_inicio")
                          .HasColumnType("time(0)")
                          .HasConversion(toProvider => TimeSpan.Parse(toProvider.ToString()),
                                         fromProvider => TimeOnly.FromTimeSpan(fromProvider));

            horarioBuilder.Property(x => x.HoraFin)
                          .HasColumnName("hora_fin")
                          .HasColumnType("time(0)")
                          .HasConversion(toProvider => TimeSpan.Parse(toProvider.ToString()),
                                         fromProvider => TimeOnly.FromTimeSpan(fromProvider));

            horarioBuilder.Property(x => x.Turno)
                          .HasColumnName("turno")
                          .HasColumnType("varchar(10)")
                          .HasConversion(toProvider => toProvider.ToString(),
                                         fromProvider => (Turno) Enum.Parse(typeof(Turno), fromProvider));
        });
    }
}
