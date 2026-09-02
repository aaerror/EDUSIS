using Domain.Curriculas.Materias.CargosDocentes;
using Domain.Curriculas.Materias;
using Domain.Curriculas;
using Domain.Cursantes;
using Domain.Docentes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EntityConfigurations;

internal class MateriasConfiguration : IEntityTypeConfiguration<Materia>
{
	public void Configure(EntityTypeBuilder<Materia> builder)
	{
		ConfigureTableMaterias(builder);
		// ConfigureTableSituacionRevista(builder);
		ConfigureTableCalificaciones(builder);
		// ConfigureTableHorarios(builder);
	}

	private void ConfigureTableMaterias(EntityTypeBuilder<Materia> builder)
	{
		builder.ToTable("materia");

		builder.Property(x => x.Id)
			   .HasColumnName("materia_id")
			   .ValueGeneratedNever();

		// PK_MATERIA
		builder.HasKey(x => x.Id)
			   .HasName("PK_MATERIA");

		// FK_CURSOS_MATERIAS
		/*builder.HasOne<Curso>()
			   .WithMany()
			   .HasPrincipalKey(x => x.Id)
			   .HasForeignKey(x => x.CursoID)
			   .HasConstraintName("FK_CURSO_MATERIA");

		builder.Property(x => x.CursoID)
			   .HasColumnName("curso_id");*/

		// FK_CURRICULA _MATERIA
		builder.Property(x => x.CurriculaID)
			   .HasColumnName("diseno_curricular_id");

		builder.HasOne<Curricula>()
			   .WithMany(x => x.Materias)
			   .HasPrincipalKey(x => x.Id)
			   .HasForeignKey(x => x.CurriculaID)
			   .HasConstraintName("FK_DISEÑO-CURRICULAR_MATERIA");

		builder.Property(x => x.Descripcion)
			   .HasColumnName("descripcion")
			   .HasColumnType("varchar(50)")
			   .IsRequired();

		builder.Property(x => x.HorasCatedra)
			   .HasColumnName("horas_catedra")
			   .HasColumnType("tinyint")
			   .IsRequired();

		// IGNORE
		builder.Ignore(x => x.DocenteID);
		builder.Ignore(x => x.Docente);
	}

	/*
	private void ConfigureTableSituacionRevista(EntityTypeBuilder<Materia> builder)
	{
		builder.OwnsMany(x => x.Docentes, builder =>
		{
			builder.ToTable("situacion_revista");

			builder.Property(x => x.Id)
				   .HasColumnName("situacion_revista_id")
				   .ValueGeneratedNever();

			// PK_SITUACIÓN-REVISTA
			builder.HasKey(x => x.Id)
				   .HasName("PK_SITUACION-REVISTA");

			// FK_MATERIAS_SITUACION-REVISTA
			builder.WithOwner()
				   .HasForeignKey("materia_id")
				   .HasConstraintName("FK_MATERIA_SITUACION-REVISTA");

			// PK_SITUACION-REVISTA
			builder.HasKey("materia_id", "situacion_revista_id")
				   .HasName("PK_SITUACION-REVISTA");

			builder.Property(x => x.DocenteID)
				   .HasColumnName("docente_id");

			// FK_PROFESORES_SITUACION-REVISTA
			builder.HasOne<Docente>()
				   .WithMany()
				   .HasForeignKey(x => x.DocenteID)
				   .HasConstraintName("FK_DOCENTE_SITUACION-REVISTA");

			builder.Property(x => x.Cargo)
				   .HasColumnName("cargo")
				   .HasColumnType("varchar(10)")
				   .HasConversion(toProvider => toProvider.ToString(),
								  fromProvider => (Cargo)Enum.Parse(typeof(Cargo), fromProvider));

			builder.OwnsOne(x => x.Periodo, static builder =>
			{
				builder.Property(x => x.FechaInicio)
					   .HasColumnName("fecha_inicio")
					   .HasColumnType("date");

				builder.Property(x => x.FechaFin)
					   .HasColumnName("fecha_fin")
					   .HasColumnType("date")
					   .IsRequired(false);
			});

			builder.Property(x => x.EnFunciones)
				   .HasColumnName("en_funciones")
				   .HasColumnType("bit")
				   .HasDefaultValue(false);
		})
			   .Navigation(nameof(Materia.Docentes))
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}
	*/

	private void ConfigureTableCalificaciones(EntityTypeBuilder<Materia> builder)
	{
		builder.OwnsMany(x => x.Calificaciones, builder =>
		{
			builder.ToTable("calificacion");

			builder.Property<int>("calificacion_id")
				   .UseIdentityColumn();

			// FK_MATERIA_CALIFICACIÓN
			builder.WithOwner()
				   .HasForeignKey("materia_id")
				   .HasConstraintName("FK_MATERIA_CALIFICACIÓN");

			// PK_CALIFICACIÓN
			builder.HasKey("materia_id", "calificacion_id")
				   .HasName("PK_CALIFICACIÓN");

			builder.Property(x => x.CursanteID)
				   .HasColumnName("cursante_id");

			// FK_CURSANTE_CALIFICACIÓN
			builder.HasOne<Cursante>()
				   .WithMany()
				   .HasForeignKey(x => x.CursanteID)
				   .HasConstraintName("FK_CURSANTE_CALIFICACIÓN");

			builder.Property(x => x.Fecha)
				   .HasColumnName("fecha")
				   .HasColumnType("date");

			builder.Property(x => x.Asistencia)
				   .HasColumnName("asistencia")
				   .HasColumnType("bit")
				   .HasDefaultValue(false);

			builder.Property(x => x.Instancia)
				   .HasColumnName("instancia")
				   .HasColumnType("varchar(10)")
				   .HasConversion(toProvider => toProvider.ToString(),
								  fromProvider => (Instancia) Enum.Parse(typeof(Instancia), fromProvider));

			builder.Property(x => x.Nota)
				   .HasColumnName("nota")
				   .HasColumnType("float(24)");

			builder.Property(x => x.Observacion)
				   .HasColumnName("observaciones")
				   .HasColumnType("varchar(10)")
				   .HasMaxLength(250)
				   .IsRequired(false);
		})
			   .Navigation(nameof(Materia.Calificaciones))
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}

	/*
	private void ConfigureTableHorarios(EntityTypeBuilder<Materia> builder)
	{
		builder.OwnsMany(m => m.Horarios, horarioBuilder =>
		{
			horarioBuilder.ToTable("horario");

			// SHADOW PROPERTY
			horarioBuilder.Property<int>("horario_id")
						  .UseIdentityColumn();

			// FK_MATERIAS_HORARIOS
			horarioBuilder.WithOwner()
						  .HasForeignKey("curso_id", "materia_id")
						  .HasConstraintName("FK_MATERIA_HORARIO");

			// PK_HORARIOS
			horarioBuilder.HasKey("curso_id", "materia_id", "horario_id")
						  .HasName("PK_HORARIO");

			horarioBuilder.Property(x => x.DiaSemana)
						  .HasColumnName("dia")
						  .HasColumnType("varchar(10)")
						  .HasConversion(toProvider => toProvider.ToString(),
										 fromProvider => (Dia)Enum.Parse(typeof(Dia), fromProvider));

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
										 fromProvider => (Turno)Enum.Parse(typeof(Turno), fromProvider));
		})
			.Navigation(x => x.Horarios)
			.UsePropertyAccessMode(PropertyAccessMode.Field);
	}
	*/
}