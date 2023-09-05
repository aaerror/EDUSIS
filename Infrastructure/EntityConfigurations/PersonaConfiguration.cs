using Domain.Personas;
using Domain.Personas.Domicilios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("Personas");

        builder.HasKey(x => x.Id);

        // ID
        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasColumnName("persona_id");
            //.HasColumnType("varchar(50)")
            /*.HasConversion(
                id => id.ToString(),
                value => Guid.Parse(value));*/

        // INFORMACIÓN PERSONAL
        builder.OwnsOne(p => p.InformacionPersonal,
            i =>
            {
                i.Property(x => x.Apellido)
                    .HasColumnName("apellido")
                    .HasColumnType("varchar(50)")
                    .IsRequired();
                i.Property(x => x.Nombre)
                    .HasColumnName("nombre")
                    .HasColumnType("varchar(50)")
                    .IsRequired();
                i.Property(x => x.Documento)
                    .HasColumnName("documento")
                    .HasColumnType("int")
                    .HasConversion(
                        dni => Int32.Parse(dni),
                        value => value.ToString())
                    .IsRequired();
                i.Property(x => x.Sexo)
                    .HasColumnName("sexo")
                    .HasColumnType("varchar(15)")
                    .HasConversion(
                        s => s.ToString(),
                        value => (Sexo) Enum.Parse(typeof(Sexo), value)
                    )
                    .IsRequired();
                i.Property(x => x.FechaNacimiento)
                    .HasColumnName("fecha_nacimiento")
                    .HasColumnType("datetime")
                    .HasConversion(
                        fecha => fecha.Date,
                        value => value.Date)
                    .IsRequired();
                i.Property(x => x.Nacionalidad)
                    .HasColumnName("nacionalidad")
                    .HasColumnType("varchar(20)")
                    .IsRequired();
            });

        // DOMICILIO
        builder.OwnsOne<Domicilio>("Domicilio",
            d =>
            {
                d.ToTable("Domicilios");
                d.WithOwner();

                // DIRECCIÓN
                d.OwnsOne<Direccion>("Direccion",
                    dir =>
                    {
                        dir.Property("Calle")
                            .HasColumnName("calle")
                            .HasColumnType("varchar(50)")
                            .IsRequired();
                        dir.Property("Altura")
                            .HasColumnName("altura")
                            .HasColumnType("int")
                            .IsRequired();
                        dir.Property<Vivienda>("Vivienda")
                            .HasColumnName("vivienda")
                            .HasColumnType("varchar(10)")
                            .HasConversion(
                                v => v.ToString(),
                                value => (Vivienda) Enum.Parse(typeof(Vivienda), value))
                            .IsRequired();
                        dir.Property("Observacion")
                            .HasColumnName("observaciones")
                            .HasColumnType("varchar(120)");
                        dir.WithOwner();
                    });

                //UBICACIÓN
                d.OwnsOne<Ubicacion>("Ubicacion",
                    u =>
                    {
                        u.Property("Localidad")
                            .HasColumnName("localidad")
                            .HasColumnType("varchar(50)")
                            .IsRequired();
                        /*u.Property("CodigoPostal")
                            .HasColumnName("codigo_postal")
                            .HasColumnType("char(4)")
                            .IsRequired();*/
                        u.Property("Provincia")
                            .HasColumnName("provincia")
                            .HasColumnType("varchar(50)")
                            .IsRequired();
                        u.Property("Pais")
                            .HasColumnName("pais")
                            .HasColumnType("varchar(50)")
                            .IsRequired();
                        u.WithOwner();
                    }) ;
            });

        // CONTACTO
        builder.Property(x => x.Telefono)
            .HasColumnName("telefono")
            .HasColumnType("varchar(15)");

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(50)");

        /*
        builder.OwnsMany(p => p.Contactos,
            c =>
            {
                c.ToTable("Contactos");

                c.WithOwner()
                    .HasForeignKey("persona_id");

                c.Property<int>("contacto_id")
                    .IsRequired();

                c.HasKey("contacto_id", "persona_id");

                c.Property(c => c.TipoContacto)
                    .HasColumnName("tipo_contacto")
                    .HasColumnType("varchar(10)")
                    .HasConversion(
                        tc => tc.ToString(),
                        value => (TipoContacto)Enum.Parse(typeof(TipoContacto), value))
                    .IsRequired();

                c.Property(c => c.Descripcion)
                    .HasColumnName("description")
                    .HasColumnType("varchar(30)")
                    .IsRequired();
            });
        */
    }
}
