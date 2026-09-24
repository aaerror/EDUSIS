using Domain.Alumnos;
using Domain.Cursantes;
using Domain.Divisiones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class CursantesConfiguration : IEntityTypeConfiguration<Cursante>
{
	public void Configure(EntityTypeBuilder<Cursante> builder)
	{
		ConfigureTableCursantes(builder);
	}

	public void ConfigureTableCursantes(EntityTypeBuilder<Cursante> builder)
	{
		builder.ToTable("cursante");

		builder.HasKey(x => x.Id)
			   .HasName("PK_CURSANTE");

		builder.Property(x => x.Id)
			   .HasColumnName("cursante_id")
			   .ValueGeneratedNever();

		builder.Property(x => x.AlumnoID)
			   .HasColumnName("alumno_id")
			   .IsRequired();

		// FK_CURSANTE_ALUMNO
		builder.HasOne<Alumno>()
			   .WithMany()
			   .HasForeignKey(x => x.AlumnoID)
			   .HasConstraintName("FK_CURSANTE_ALUMNO")
			   .OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.DivisionID)
			   .HasColumnName("division_id")
			   .IsRequired();

		// FK_CURSANTE_DIVISION
		builder.HasOne<Division>()
			   .WithMany()
			   .HasForeignKey(x => x.DivisionID)
			   .HasConstraintName("FK_CURSANTE_DIVISION")
			   .OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.FechaInicio)
			   .HasColumnName("fecha_inicio")
			   .HasColumnType("date")
			   .IsRequired();

		builder.Property(x => x.FechaFin)
			   .HasColumnName("fecha_fin")
			   .HasColumnType("date")
			   .IsRequired(false);

		builder.Property(x => x.EsRecursante)
			   .HasColumnName("es_recursante")
			   .HasColumnType("bit")
			   .IsRequired();

		// CicloLectivo es VO: se mapea con OwnsOne, y su Periodo dobla como columna propia
		// y como FK hacia el catálogo ciclo_lectivo (ver CicloLectivoConfiguration).
		builder.OwnsOne(x => x.CicloLectivo, ciclo =>
		{
			ciclo.Property(x => x.Periodo)
				 .HasColumnName("ciclo_lectivo")
				 .HasColumnType("smallint")
				 .HasConversion(
					 toProvider => short.Parse(toProvider),
					 fromProvider => fromProvider.ToString())
				 .IsRequired();

			// FK_CURSANTE_CICLO_LECTIVO. Sobrecarga (relatedTypeName, navigationName): CicloLectivo
			// no tiene una navegación de dominio hacia el catálogo, por eso navigationName va en null.
			ciclo.HasOne(CicloLectivoConfiguration.CATALOGO, navigationName: null)
				 .WithMany()
				 .HasForeignKey(nameof(CicloLectivo.Periodo))
				 .HasPrincipalKey("periodo")
				 .HasConstraintName("FK_CURSANTE_CICLO_LECTIVO")
				 .OnDelete(DeleteBehavior.Restrict);
		});

		// FK_CALIFICACION_CURSANTE (FK sombra "CursanteID", ver CalificacionesConfiguration)
		builder.HasMany(x => x.Calificaciones)
			   .WithOne()
			   .HasForeignKey("CursanteID")
			   .HasConstraintName("FK_CALIFICACION_CURSANTE")
			   .OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(x => x.Calificaciones)
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}

	/*
	public void ConfigureTableAsistencias(EntityTypeBuilder<Cursante> builder)
	{
		builder.OwnsMany(x => x.Asistencias, asistenciasBuilder =>
		{
			asistenciasBuilder.ToTable("asistencias");

			asistenciasBuilder.Property<int>("asistencia_id");

			// FK_ASISTENCIAS_CURSANTES
			asistenciasBuilder.WithOwner()
							  .HasForeignKey("cursante_id")
							  .HasForeignKey("FK_ASISTENCIAS_CURSANTES");

			// PK_ASISTENCIAS
			asistenciasBuilder.HasKey("asistencia_id", "cursante_id")
							  .HasName("PK_ASISTENCIAS");
		})
			   .Navigation(nameof(Cursante.Asistencias))
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}
	*/
}
