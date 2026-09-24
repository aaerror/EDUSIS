using Domain.Alumnos;
using Domain.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

/* https://github.com/ardalis/awesome-ddd?tab=readme-ov-file#sample-projects */
internal class AlumnosConfiguration : IEntityTypeConfiguration<Alumno>
{
	public void Configure(EntityTypeBuilder<Alumno> builder)
	{
		ConfigureTableAlumnos(builder);
	}

	private void ConfigureTableAlumnos(EntityTypeBuilder<Alumno> builder)
	{
		builder.HasBaseType<Persona>();

		builder.ToTable("alumno");

		// PK/FK de herencia TPT: ver nota en PersonasConfiguration.ConfigureTablePersonas.
		// No se puede nombrar la PK por tabla (HasKey no admite llamarse en un tipo
		// derivado: EF lanza InvalidOperationException) ni la FK de herencia
		// alumno->persona (no existe como IMutableForeignKey durante Configure();
		// EF la crea recién al finalizar el modelo). Quedan con el nombre de EF
		// por convención (PK_alumno / FK_alumno_persona_persona_id).
		builder.Property(a => a.Legajo)
			   .HasColumnName("legajo")
			   .HasMaxLength(6)
			   .IsUnicode(false)
			   .IsRequired();

		builder.OwnsOne(x => x.Periodo, periodoBuilder => periodoBuilder.ConfigurarPeriodo());
	}
}