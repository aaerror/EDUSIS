using Domain.Docentes;
using Domain.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class DocentesConfigurations : IEntityTypeConfiguration<Docente>
{
    public void Configure(EntityTypeBuilder<Docente> builder)
    {
        builder.HasBaseType(typeof(Persona));

        builder.ToTable("docentes");

        builder.Property(x => x.Legajo)
               .HasColumnName("legajo")
               .HasColumnType("varchar(5)")
               .IsRequired();

        builder.Property(x => x.FechaAltaInstitucion)
               .HasColumnName("fecha_alta_institucion")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(x => x.FechaBajaInstitucion)
               .HasColumnName("fecha_baja_institucion")
               .HasColumnType("date");

        builder.Property(x => x.EstaActivo)
               .HasColumnName("esta_activo")
               .HasColumnType("bit");

        builder.OwnsMany(x => x.Puestos, puestosBuilder =>
        {
            puestosBuilder.ToTable("puestos");

            puestosBuilder.Property<int>("puesto_id")
                          .UseIdentityColumn();

            puestosBuilder.WithOwner()
                          .HasForeignKey("docente_id")
                          .HasConstraintName("FK_DOCENTES_PUESTOS");

            puestosBuilder.HasKey("puesto_id", "docente_id")
                          .HasName("PK_PUESTOS");

            puestosBuilder.Property(x => x.Posicion)
                          .HasColumnName("posicion")
                          .HasColumnType("varchar(15)")
                          .HasConversion(cargo => cargo.ToString(), value => (Posicicion) Enum.Parse(typeof(Posicicion), value));

            puestosBuilder.Property(x => x.FechaAlta)
                          .HasColumnName("fecha_alta")
                          .IsRequired();

            puestosBuilder.Property(x => x.FechaBaja)
                          .HasColumnName("fecha_baja");
        });

        builder.Metadata.FindNavigation(nameof(Docente.Puestos))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
