using Domain.Personas;
using Domain.Personas.Domicilios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class PersonasConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        ConfigureTablePersonas(builder);
        ConfigureTableDomicilios(builder);
    }

    private void ConfigureTablePersonas(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("persona");

        // PK_PERSONA
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasColumnName("persona_id")
               .ValueGeneratedNever();

        #region Información Personal
        builder.OwnsOne(p => p.InformacionPersonal, informacionPersonalBuilder =>
        {
            informacionPersonalBuilder.Property(x => x.Apellido)
                                      .HasColumnName("apellido")
                                      .HasColumnType("varchar(50)")
                                      .IsRequired();

            informacionPersonalBuilder.Property(x => x.Nombre)
                                      .HasColumnName("nombre")
                                      .HasColumnType("varchar(50)")
                                      .IsRequired();

            informacionPersonalBuilder.Property(x => x.Documento)
                                      .HasColumnName("documento")
                                      .HasColumnType("int")
                                      .HasConversion(toProvider => Int32.Parse(toProvider),
                                                     fromProvider => fromProvider.ToString())
                                      .IsRequired();

            informacionPersonalBuilder.Property(x => x.Sexo)
                                      .HasColumnName("sexo")
                                      .HasColumnType("varchar(15)")
                                      .HasConversion(toProvider => toProvider.ToString(),
                                                     fromProvider => (Sexo) Enum.Parse(typeof(Sexo), fromProvider))
                                      .IsRequired();

            informacionPersonalBuilder.Property(x => x.FechaNacimiento)
                                      .HasColumnName("fecha_nacimiento")
                                      .HasColumnType("date")
                                      .HasConversion(toProvider => toProvider.Date,
                                                     fromProvider => fromProvider.Date)
                                      .IsRequired();

            informacionPersonalBuilder.Property(x => x.Nacionalidad)
                                      .HasColumnName("nacionalidad")
                                      .HasColumnType("varchar(20)")
                                      .IsRequired();

            informacionPersonalBuilder.HasIndex(x => x.Documento)
                                      .IsUnique();
        });
        #endregion

        #region Contacto
        builder.Property(x => x.Telefono)
               .HasColumnName("telefono")
               .HasColumnType("varchar(15)");

        builder.Property(x => x.Email)
               .HasColumnName("email")
               .HasColumnType("varchar(50)");

        builder.HasIndex(x => x.Email)
               .IsUnique();
        #endregion
    }

    private void ConfigureTableDomicilios(EntityTypeBuilder<Persona> builder)
    {
        builder.OwnsOne(x => x.Domicilio, domiciliosBuilder =>
        {
            domiciliosBuilder.ToTable("domicilio");

            // FK_PERSONA_DOMICILIO
            domiciliosBuilder.WithOwner()
                             .HasForeignKey("persona_id")
                             .HasConstraintName("FK_PERSONA_DOMICILIO");

            // PK_DOMICILIO
            domiciliosBuilder.HasKey("persona_id")
                             .HasName("PK_DOMICILIO");

            #region Dirección
            domiciliosBuilder.OwnsOne(x => x.Direccion, direccionBuilder =>
            {
                direccionBuilder.Property("Calle")
                                .HasColumnName("calle")
                                .HasColumnType("varchar(50)")
                                .IsRequired();

                direccionBuilder.Property("Altura")
                                .HasColumnName("altura")
                                .HasColumnType("varchar(6)")
                                .IsRequired(false);

                direccionBuilder.Property<Vivienda>("Vivienda")
                                .HasColumnName("vivienda")
                                .HasColumnType("varchar(20)")
                                .HasConversion(toProvider => toProvider.ToString(),
                                               fromProvider => (Vivienda) Enum.Parse(typeof(Vivienda), fromProvider))
                                .IsRequired();

                direccionBuilder.Property("Observacion")
                                .HasColumnName("observaciones")
                                .HasColumnType("varchar(120)");
            });
            #endregion

            #region Ubicación
            domiciliosBuilder.OwnsOne(x => x.Ubicacion, ubicacionBuilder =>
            {
                ubicacionBuilder.WithOwner();

                ubicacionBuilder.Property("Localidad")
                                .HasColumnName("localidad")
                                .HasColumnType("varchar(50)")
                                .IsRequired();

                /**
                 * u.Property("CodigoPostal")
                 *  .HasColumnName("codigo_postal")
                 *  .HasColumnType("char(4)")
                 *  .IsRequired();
                 **/

                ubicacionBuilder.Property("Provincia")
                                .HasColumnName("provincia")
                                .HasColumnType("varchar(50)")
                                .IsRequired();

                ubicacionBuilder.Property("Pais")
                                .HasColumnName("pais")
                                .HasColumnType("varchar(50)")
                                .IsRequired();
            });
            #endregion
        });
    }
}