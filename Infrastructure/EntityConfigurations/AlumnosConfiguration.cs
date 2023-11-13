using Domain.Alumnos;
using Domain.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class AlumnosConfiguration : IEntityTypeConfiguration<Alumno>
{
    public void Configure(EntityTypeBuilder<Alumno> builder)
    {
        builder.HasBaseType<Persona>();

        builder.ToTable("alumnos");

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
