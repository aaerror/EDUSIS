using Domain.Cursos;
using Domain.Cursos.Materias;
using Domain.Docentes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class MateriasConfiguration : IEntityTypeConfiguration<Materia>
{
    public void Configure(EntityTypeBuilder<Materia> builder)
    {
        builder.ToTable("materias");

        // PK_MATERIAS
        builder.HasKey(x => x.Id)
               .HasName("PK_MATERIAS");

        builder.Property(x => x.Id)
               .HasColumnName("materia_id")
               .ValueGeneratedNever();

        // FK_CURSOS_MATERIAS
        builder.HasOne<Curso>()
               .WithMany(x => x.Materias)
               .HasPrincipalKey(x => x.Id)
               .HasForeignKey(x => x.CursoID)
               .HasConstraintName("FK_CURSOS_MATERIAS");

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
                                   .HasConstraintName("FK_MATERIAS_SITUACION-REVISTA");

            // PK_SITUACION-REVISTA
            situacionRevistaBuilder.HasKey("materia_id", "situacion_revista_id")
                                   .HasName("PK_SITUACION-REVISTA");


            situacionRevistaBuilder.Property(x => x.ProfesorId)
                                   .HasColumnName("profesor_id");

            // FK_PROFESORES_SITUACION-REVISTA
            situacionRevistaBuilder.HasOne<Docente>()
                                   .WithMany()
                                   .HasForeignKey(x => x.ProfesorId)
                                   .HasConstraintName("FK_DOCENTES_SITUACION-REVISTA");
            /*.WithOne()
            .HasForeignKey<SituacionRevista>(x => x.ProfesorId)
            .HasConstraintName("FK_DOCENTES_SITUACION-REVISTA");*/

            situacionRevistaBuilder.Property(x => x.Cargo)
                                   .HasColumnName("cargo")
                                   .HasColumnType("varchar(10)")
                                   .HasConversion(cargo => cargo.ToString(), value => (Cargo)Enum.Parse(typeof(Cargo), value));

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
            horarioBuilder.ToTable("horarios");

            // SHADOW PROPERTY
            horarioBuilder.Property<int>("horario_id")
                          .UseIdentityColumn();

            // FK_MATERIAS_HORARIOS
            horarioBuilder.WithOwner()
                          .HasForeignKey("materia_id")
                          .HasConstraintName("FK_MATERIAS_HORARIOS");

            // PK_HORARIOS
            horarioBuilder.HasKey("materia_id", "horario_id")
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
                          .HasConversion(turno => turno.ToString(), value => (Turno)Enum.Parse(typeof(Turno), value));
        });
    }
}
