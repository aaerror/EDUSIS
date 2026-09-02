using Domain.Alumnos;
using Domain.Cursantes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class CursantesConfiguration : IEntityTypeConfiguration<Cursante>
{
	public void Configure(EntityTypeBuilder<Cursante> builder)
	{
		ConfigureTableCursantes(builder);
		ConfigureTableAsistencias(builder);
	}

	public void ConfigureTableCursantes(EntityTypeBuilder<Cursante> builder)
	{
		builder.ToTable("cursantes");

		builder.HasKey(x => x.Id)
			   .HasName("PK_CURSANTES");

		builder.Property(x => x.Id)
			   .HasColumnName("cursante_id")
			   .ValueGeneratedNever();

		// FK_CICLO-LECTIVO_CURSANTES
		builder.HasOne<CicloLectivo>(x => x.CicloLectivo)
			   .WithMany()
			   .HasForeignKey("ciclo_lectivo_id")
			   .HasConstraintName("FK_CICLO-LECTIVO_CURSANTES");

		// FK_ALUMNOS_CURSANTES
		builder.HasOne<Alumno>()
			   .WithOne()
			   .HasForeignKey<Cursante>(x => x.AlumnoID)
			   .HasConstraintName("FK_ALUMNOS_CURSANTES");

		builder.Property(x => x.AlumnoID)
			   .HasColumnName("alumno_id")
			   .IsRequired();

		builder.Property(x => x.FechaInicio)
			   .HasColumnName("fecha_inicio")
			   .HasColumnType("date")
			   .IsRequired();

		builder.Property(x => x.FechaFin)
			   .HasColumnName("fecha_fin")
			   .HasColumnType("date")
			   .IsRequired(false);

		builder.Ignore(x => x.Ausencias);
		builder.Ignore(x => x.Inasistencias);
		builder.Ignore(x => x.Tardanzas);
	}

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
}