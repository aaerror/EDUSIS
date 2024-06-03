﻿using Domain.Personas;
using Domain.Personas.Domicilios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class PersonasConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("persona");

        builder.HasKey(x => x.Id);

        // ID
        builder.Property(x => x.Id)
               .HasColumnName("persona_id")
               .ValueGeneratedNever();
            //.HasColumnType("varchar(50)")
            /*.HasConversion(
                id => id.ToString(),
                value => Guid.Parse(value));*/

        // INFORMACIÓN PERSONAL
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

        // DOMICILIO
        builder.OwnsOne(x => x.Domicilio, domiciliosBuilder =>
        {
            domiciliosBuilder.ToTable("domicilio");

            domiciliosBuilder.WithOwner()
                             .HasForeignKey("persona_id")
                             .HasConstraintName("FK_PERSONA_DOMICILIO");

            domiciliosBuilder.HasKey("persona_id")
                             .HasName("PK_DOMICILIO");

            // DIRECCIÓN
            domiciliosBuilder.OwnsOne(x => x.Direccion, direccionBuilder =>
            {
                //direccionBuilder.WithOwner();

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

            // UBICACIÓN
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
                    */

                ubicacionBuilder.Property("Provincia")
                                .HasColumnName("provincia")
                                .HasColumnType("varchar(50)")
                                .IsRequired();

                ubicacionBuilder.Property("Pais")
                                .HasColumnName("pais")
                                .HasColumnType("varchar(50)")
                                .IsRequired();
            });
        });

        // CONTACTO
        builder.Property(x => x.Telefono)
               .HasColumnName("telefono")
               .HasColumnType("varchar(15)");

        builder.Property(x => x.Email)
               .HasColumnName("email")
               .HasColumnType("varchar(50)");

        builder.HasIndex(x => x.Email)
               .IsUnique();
    }
}
