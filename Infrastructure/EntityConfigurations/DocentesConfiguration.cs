using Domain.Docentes;
using Domain.Docentes.Licencias;
using Domain.Personas;
using Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class DocentesConfiguration : IEntityTypeConfiguration<Docente>
{
    public void Configure(EntityTypeBuilder<Docente> builder)
    {
        ConfigureTableDocentes(builder);
        ConfigureTableLicencias(builder);
        ConfigureTablePuestos(builder);
    }

    private void ConfigureTableDocentes(EntityTypeBuilder<Docente> builder)
    {
        builder.HasBaseType(typeof(Persona));

        builder.ToTable("docente");

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

        /*builder.Property(x => x.EstaActivo)
               .HasColumnName("esta_activo")
               .HasColumnType("bit");*/

        builder.HasOne<Usuario>()
               .WithOne()
               .HasForeignKey<Usuario>(x => x.DocenteID)
               .HasConstraintName("FK_USUARIO_DOCENTE")
               .IsRequired(false);

        builder.Ignore(x => x.EstaActivo);

        builder.Metadata.FindNavigation(nameof(Docente.Licencias))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Docente.Puestos))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);
    }

    private void ConfigureTableLicencias(EntityTypeBuilder<Docente> builder)
    {
        builder.OwnsMany(x => x.Licencias, licenciasBuilder =>
        {
            licenciasBuilder.ToTable("licencia");

            licenciasBuilder.Property<int>("licencia_id")
                            .UseIdentityColumn();

            licenciasBuilder.WithOwner()
                            .HasForeignKey("docente_id")
                            .HasConstraintName("FK_DOCENTE_LICENCIA");

            licenciasBuilder.HasKey("licencia_id", "docente_id")
                            .HasName("PK_LICENCIA");

            licenciasBuilder.Property(x => x.Articulo)
                            .HasColumnName("articulo")
                            .HasColumnType("varchar(15)")
                            .HasConversion(toProvider => toProvider.ToString(),
                                           fromProvider => (Articulo) Enum.Parse(typeof(Articulo), fromProvider));

            licenciasBuilder.Property(x => x.Estado)
                            .HasColumnName("estado")
                            .HasColumnType("varchar(15)")
                            .HasConversion(toProvider => toProvider.ToString(),
                                           fromProvider => (Estado) Enum.Parse(typeof(Estado), fromProvider));

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
    }

    private void ConfigureTablePuestos(EntityTypeBuilder<Docente> builder)
    {
        builder.OwnsMany(x => x.Puestos, puestosBuilder =>
        {
            puestosBuilder.ToTable("puesto");

            puestosBuilder.Property<int>("puesto_id")
                          .UseIdentityColumn();

            puestosBuilder.WithOwner()
                          .HasForeignKey("docente_id")
                          .HasConstraintName("FK_DOCENTE_PUESTO");

            puestosBuilder.HasKey("puesto_id", "docente_id")
                          .HasName("PK_PUESTO");

            puestosBuilder.Property(x => x.Posicion)
                          .HasColumnName("posicion")
                          .HasColumnType("varchar(15)")
                          .HasConversion(toProvider => toProvider.ToString(),
                                         fromProvider => (Posicion)Enum.Parse(typeof(Posicion), fromProvider));

            puestosBuilder.Property(x => x.FechaInicio)
                          .HasColumnName("fecha_inicio")
                          .HasColumnType("date");

            puestosBuilder.Property(x => x.FechaFin)
                          .HasColumnName("fecha_fin")
                          .HasColumnType("date")
                          .IsRequired(false);
        });
    }
}
