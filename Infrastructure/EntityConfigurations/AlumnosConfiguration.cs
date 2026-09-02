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

		builder.Property(a => a.Legajo)
			   .HasColumnName("legajo")
			   .IsRequired();

		builder.OwnsOne(x => x.Periodo, periodoBuilder =>
		{
			periodoBuilder.Property(a => a.FechaInicio)
						  .HasColumnName("fecha_inicio")
						  .HasColumnType("date")
						  .IsRequired();

			periodoBuilder.Property(a => a.FechaFin)
						  .HasColumnName("fecha_fin")
						  .HasColumnType("date")
						  .IsRequired(false);
		});
	}
}