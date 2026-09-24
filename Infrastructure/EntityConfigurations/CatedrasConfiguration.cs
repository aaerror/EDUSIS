using Domain.Catedras;
using Domain.Catedras.Horarios;
using Domain.Divisiones;
using Domain.Materias;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class CatedrasConfiguration : IEntityTypeConfiguration<Catedra>
{
	public void Configure(EntityTypeBuilder<Catedra> builder)
	{
		ConfigureTableCatedras(builder);
		ConfigureTableHorarios(builder);
		ConfigureTableSituacionesRevista(builder);
	}

	private void ConfigureTableCatedras(EntityTypeBuilder<Catedra> builder)
	{
		builder.ToTable("catedra");

		builder.Property(x => x.Id)
			   .HasColumnName("catedra_id")
			   .ValueGeneratedNever();

		// PK_CATEDRA
		builder.HasKey(x => x.Id)
			   .HasName("PK_CATEDRA");

		builder.Property(x => x.MateriaID)
			   .HasColumnName("materia_id");

		// FK_CATEDRA_MATERIA
		builder.HasOne<Materia>()
			   .WithMany()
			   .HasForeignKey(x => x.MateriaID)
			   .OnDelete(DeleteBehavior.Restrict)
			   .HasConstraintName("FK_CATEDRA_MATERIA");

		builder.Property(x => x.DivisionID)
			   .HasColumnName("division_id");

		// FK_CATEDRA_DIVISION
		builder.HasOne<Division>()
			   .WithMany()
			   .HasForeignKey(x => x.DivisionID)
			   .OnDelete(DeleteBehavior.Restrict)
			   .HasConstraintName("FK_CATEDRA_DIVISION");

		// ÍNDICE ÚNICO: a lo sumo una cátedra por materia y división
		builder.HasIndex(x => new { x.MateriaID, x.DivisionID })
			   .IsUnique();

		// CargaHoraria es una instantánea de la currícula vigente: columna simple, no FK.
		builder.Property(x => x.CargaHoraria)
			   .HasColumnName("carga_horaria")
			   .IsRequired();

		builder.Property(x => x.SituacionEnFuncionesID)
			   .HasColumnName("situacion_en_funciones_id")
			   .IsRequired(false);

		// FK_CATEDRA_SITUACION_REVISTA: FK compuesta contra la clave alterna (catedra_id, situacion_revista_id)
		// de SituacionRevista, para garantizar en BD que la situación "en funciones" pertenezca a esta misma cátedra.
		// Declarada en SituacionRevistaConfiguration (después de la propiedad sombra "CatedraID" y la clave
		// alterna), porque ApplyConfigurationsFromAssembly no garantiza el orden de aplicación de las configs.

		// IGNORE: calculadas a partir de Horarios/CargaHoraria
		builder.Ignore(x => x.HorasAsignadas);
		builder.Ignore(x => x.HorasSinAsignar);
	}

	private void ConfigureTableHorarios(EntityTypeBuilder<Catedra> builder)
	{
		builder.OwnsMany(x => x.Horarios, horarioBuilder =>
		{
			horarioBuilder.ToTable("horario");

			// SHADOW PROPERTY
			horarioBuilder.Property<int>("horario_id")
						  .UseIdentityColumn();

			// FK_HORARIO_CATEDRA
			horarioBuilder.WithOwner()
						  .HasForeignKey("catedra_id")
						  .HasConstraintName("FK_HORARIO_CATEDRA");

			// PK_HORARIO
			horarioBuilder.HasKey("catedra_id", "horario_id")
						  .HasName("PK_HORARIO");

			horarioBuilder.Property(x => x.Turno)
						  .HasColumnName("turno")
						  .HasColumnType("varchar(10)")
						  .HasConversion(toProvider => toProvider.ToString(),
										 fromProvider => (Turno) Enum.Parse(typeof(Turno), fromProvider));

			horarioBuilder.Property(x => x.DiaSemana)
						  .HasColumnName("dia")
						  .HasColumnType("varchar(10)")
						  .HasConversion(toProvider => toProvider.ToString(),
										 fromProvider => (Dia) Enum.Parse(typeof(Dia), fromProvider));

			// EF 7 no mapea TimeOnly nativamente en SQL Server: se convierte vía TimeSpan.
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
		});

		builder.Navigation(x => x.Horarios)
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}

	private void ConfigureTableSituacionesRevista(EntityTypeBuilder<Catedra> builder)
	{
		// FK_SITUACION_REVISTA_CATEDRA: entidad interna, tabla propia (ver SituacionRevistaConfiguration).
		builder.HasMany(x => x.SituacionesRevista)
			   .WithOne()
			   .HasPrincipalKey(x => x.Id)
			   .HasForeignKey("CatedraID")
			   .OnDelete(DeleteBehavior.Cascade)
			   .HasConstraintName("FK_SITUACION_REVISTA_CATEDRA");

		builder.Navigation(x => x.SituacionesRevista)
			   .HasField("_situaciones")
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
