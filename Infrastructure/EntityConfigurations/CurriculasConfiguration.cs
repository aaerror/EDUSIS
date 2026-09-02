using Domain.Curriculas;
using Domain.Cursos;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EntityConfigurations;

internal class CurriculasConfiguration : IEntityTypeConfiguration<Curricula>
{
	public void Configure(EntityTypeBuilder<Curricula> builder)
	{
		ConfigureTableCurriculas(builder);
		//ConfigureTableMaterias(builder);
	}

	public void ConfigureTableCurriculas(EntityTypeBuilder<Curricula> builder)
	{
		builder.ToTable("diseno_curricular");

		//builder.HasKey(x => new { x.CursoID, x.Id });
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			   .HasColumnName("diseno_curricular_id")
			   .ValueGeneratedNever();

		// FK_CURSO_CURRICULA
		builder.HasOne<Curso>()
			   .WithMany()
			   .HasPrincipalKey(x => x.Id)
			   .HasForeignKey(x => x.CursoID)
			   .HasConstraintName("FK_CURSO_DISEÑO-CURRICULAR");

		builder.Property(x => x.CursoID)
			   .HasColumnName("curso_id");

		// PERÍODO
		builder.OwnsOne(x => x.Periodo, static periodoBuilder =>
		{
			periodoBuilder.Property(x => x.FechaInicio)
						  .HasColumnName("fecha_inicio")
						  .HasColumnType("date");

			periodoBuilder.Property(x => x.FechaFin)
						  .HasColumnName("fecha_fin")
						  .HasColumnType("date")
						  .IsRequired(false);
		});

		// FK_MATERIA_CURRICULA
		/*builder.HasMany<Materia>()
			   .WithOne()
			   .HasPrincipalKey(x => x.Id)
			   .HasForeignKey(x => x.CurriculaID);*/

		builder.Navigation(nameof(Curricula.Materias))
			   .UsePropertyAccessMode(PropertyAccessMode.Field);

		// IGNORE
		builder.Ignore(x => x.TotalEspacios);
		builder.Ignore(x => x.TotalHorasSemanales);

		/*
		builder.OwnsMany(m => m.Materias, materiaBuilder =>
		{
			materiaBuilder.ToTable("materia");

			materiaBuilder.WithOwner()
						  .HasForeignKey("curso_id", "diseno_curricular_id")
						  .HasConstraintName("FK_MATERIA_DISENO-CURRICULAR");

			materiaBuilder.Property(x => x.Id)
						  .HasColumnName("materia_id")
						  .ValueGeneratedNever();

			// PK_MATERIAS
			materiaBuilder.HasKey("curso_id", "diseno_curricular_id", "Id")
						  .HasName("PK_MATERIA");

			materiaBuilder.Property(x => x.Descripcion)
						  .HasColumnName("descripcion")
						  .HasColumnType("varchar(50)")
						  .IsRequired();

			materiaBuilder.Property(x => x.HorasCatedra)
						  .HasColumnName("horas_catedra")
						  .HasColumnType("tinyint")
						  .IsRequired();
			// IGNORE
			materiaBuilder.Ignore(x => x.DocenteID);

			// SITUACION REVISTA
			materiaBuilder.OwnsMany(sr => sr.Docentes, situacionRevistaBuilder =>
			{
				situacionRevistaBuilder.ToTable("situacion_revista");

				// SHADOW PROPERTY
				situacionRevistaBuilder.Property<int>("situacion_revista_id")
									   .UseIdentityColumn();

				// FK_MATERIAS_SITUACION-REVISTA
				situacionRevistaBuilder.WithOwner()
									   .HasForeignKey("curso_id", "diseno_curricular_id", "materia_id")
									   .HasConstraintName("FK_MATERIA_SITUACION-REVISTA");

				// PK_SITUACION-REVISTA
				situacionRevistaBuilder.HasKey("curso_id", "diseno_curricular_id", "materia_id", "situacion_revista_id")
									   .HasName("PK_SITUACION-REVISTA");


				situacionRevistaBuilder.Property(x => x.DocenteID)
									   .HasColumnName("profesor_id");

				// FK_PROFESORES_SITUACION-REVISTA
				situacionRevistaBuilder.HasOne<Docente>()
									   .WithMany()
									   .HasForeignKey(x => x.DocenteID)
									   .HasConstraintName("FK_DOCENTE_SITUACION-REVISTA");

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
									   .HasColumnType("bit")
									   .HasDefaultValue(false);
			})
						  .Navigation(x => x.Docentes)
						  .UsePropertyAccessMode(PropertyAccessMode.Field);

			// HORARIOS
			materiaBuilder.OwnsMany(m => m.Horarios, horarioBuilder =>
			{
				horarioBuilder.ToTable("horario");

				// SHADOW PROPERTY
				horarioBuilder.Property<int>("horario_id")
							  .UseIdentityColumn();

				// FK_MATERIAS_HORARIOS
				horarioBuilder.WithOwner()
							  .HasForeignKey("curso_id", "diseno_curricular_id", "materia_id")
							  .HasConstraintName("FK_MATERIA_HORARIO");

				// PK_HORARIOS
				horarioBuilder.HasKey("curso_id", "diseno_curricular_id", "materia_id", "horario_id")
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
		})
			   .Navigation(x => x.Materias)
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
		*/
	}

	/*
	public void ConfigureTableMaterias(EntityTypeBuilder<Curricula> builder)
	{
		builder.OwnsMany(x => x.Materias, materiaBuilder =>
		{
			materiaBuilder.ToTable("materia");

			materiaBuilder.WithOwner()
						  .HasForeignKey("diseno_curricular_id")
						  .HasConstraintName("FK_MATERIA_DISENO-CURRICULAR");

			materiaBuilder.Property(x => x.Id)
						  .HasColumnName("materia_id")
						  .ValueGeneratedNever();

			// PK_MATERIAS
			materiaBuilder.HasKey("diseno_curricular_id", "Id")
						  .HasName("PK_MATERIA");

			materiaBuilder.Property(x => x.Descripcion)
						  .HasColumnName("descripcion")
						  .HasColumnType("varchar(50)")
						  .IsRequired();

			materiaBuilder.Property(x => x.HorasCatedra)
						  .HasColumnName("horas_catedra")
						  .HasColumnType("tinyint")
						  .IsRequired();

			// SITUACIÓN REVISTA
			materiaBuilder.OwnsMany(sr => sr.Docentes, situacionRevistaBuilder =>
			{
				situacionRevistaBuilder.ToTable("situacion_revista");

				// SHADOW PROPERTY
				situacionRevistaBuilder.Property<int>("situacion_revista_id")
									   .UseIdentityColumn();

				// FK_MATERIAS_SITUACION-REVISTA
				situacionRevistaBuilder.WithOwner()
									   .HasForeignKey("diseno_curricular_id", "materia_id")
									   .HasConstraintName("FK_MATERIA_SITUACION-REVISTA");

				// PK_SITUACION-REVISTA
				situacionRevistaBuilder.HasKey("diseno_curricular_id", "materia_id", "situacion_revista_id")
									   .HasName("PK_SITUACION-REVISTA");


				situacionRevistaBuilder.Property(x => x.DocenteID)
									   .HasColumnName("profesor_id");

				// FK_PROFESORES_SITUACION-REVISTA
				situacionRevistaBuilder.HasOne<Docente>()
									   .WithMany()
									   .HasForeignKey(x => x.DocenteID)
									   .HasConstraintName("FK_DOCENTE_SITUACION-REVISTA");

				situacionRevistaBuilder.Property(x => x.Cargo)
									   .HasColumnName("cargo")
									   .HasColumnType("varchar(10)")
									   .HasConversion(toProvider => toProvider.ToString(),
													  fromProvider => (Cargo) Enum.Parse(typeof(Cargo), fromProvider));

				situacionRevistaBuilder.Property(x => x.FechaAlta)
									   .HasColumnName("fecha_alta")
									   .HasColumnType("date");

				situacionRevistaBuilder.Property(x => x.FechaBaja)
									   .HasColumnName("fecha_baja")
									   .HasColumnType("date")
									   .IsRequired(false);

				situacionRevistaBuilder.Property(x => x.EnFunciones)
									   .HasColumnName("en_funciones")
									   .HasColumnType("bit")
									   .HasDefaultValue(false);
			})
						  .Navigation(x => x.Docentes)
						  .UsePropertyAccessMode(PropertyAccessMode.Field);

			// IGNORE
			materiaBuilder.Ignore(x => x.DocenteID);
		})
			   .Navigation(x => x.Materias)
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}
	*/
}