using Domain.Docentes;
using Domain.Docentes.Licencias;
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
               .HasColumnType("varchar(6)")
               .IsRequired();

        builder.Property(x => x.CUIL)
               .HasColumnName("cuil")
               .HasColumnType("varchar(11)")
               .IsRequired();

        builder.Property(x => x.FechaAlta)
               .HasColumnName("fecha_alta")
               .HasColumnType("date")
               .IsRequired();

        builder.Property(x => x.FechaBaja)
               .HasColumnName("fecha_baja")
               .HasColumnType("date")
               .IsRequired(false);

        builder.Property(x => x.EstaActivo)
               .HasColumnName("esta_activo")
               .HasColumnType("bit");

        builder.OwnsMany(x => x.Licencias, licenciasBuilder =>
        {
            licenciasBuilder.ToTable("licencias");

            licenciasBuilder.Property<int>("licencia_id")
                            .UseIdentityColumn();

            licenciasBuilder.WithOwner()
                            .HasForeignKey("docente_id")
                            .HasConstraintName("FK_DOCENTES_LICENCIAS");

            licenciasBuilder.HasKey("licencia_id", "docente_id")
                            .HasName("PK_LICENCIAS");

            licenciasBuilder.Property(x => x.Articulo)
                            .HasColumnName("articulo")
                            .HasColumnType("varchar(15)")
                            .HasConversion(articulo => articulo.ToString(), value => (Articulo) Enum.Parse(typeof(Articulo), value));

            licenciasBuilder.Property(x => x.Estado)
                            .HasColumnName("estado")
                            .HasColumnType("varchar(15)")
                            .HasConversion(estado => estado.ToString(), value => (Estado) Enum.Parse(typeof(Estado), value));

            licenciasBuilder.Property(x => x.FechaInicio)
                            .HasColumnName("fecha_inicio")
                            .HasColumnType("date");

            licenciasBuilder.Property(x => x.Dias)
                            .HasColumnName("dias");

            licenciasBuilder.Property(x => x.Observacion)
                            .HasColumnName("observacion")
                            .HasColumnType("varchar(10)")
                            .HasMaxLength(120);

            licenciasBuilder.Ignore(x => x.FechaFin);
        });

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
                          .HasConversion(cargo => cargo.ToString(), value => (Posicion) Enum.Parse(typeof(Posicion), value));

            puestosBuilder.Property(x => x.FechaInicio)
                          .HasColumnName("fecha_inicio")
                          .HasColumnType("date");

            puestosBuilder.Property(x => x.FechaFin)
                          .HasColumnName("fecha_fin")
                          .HasColumnType("date")
                          .IsRequired(false);
        });

        builder.Metadata.FindNavigation(nameof(Docente.Licencias))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Docente.Puestos))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
