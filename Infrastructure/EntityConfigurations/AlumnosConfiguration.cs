using Domain.Alumno;
using Domain.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class AlumnosConfiguration : IEntityTypeConfiguration<Alumno>
{
    public void Configure(EntityTypeBuilder<Alumno> builder)
    {
        builder.HasBaseType<Persona>();

        builder.ToTable("Alumnos");

        builder.Property(a => a.Legajo)
            .HasColumnName("legajo")
            .IsRequired();
    }
}
