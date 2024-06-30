using Domain.Alumnos;
using Domain.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

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

        builder.Property(a => a.FechaAlta)
               .HasColumnName("fecha_alta")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(a => a.FechaBaja)
               .HasColumnName("fecha_baja")
               .HasColumnType("date")
               .IsRequired(false);
    }
}
