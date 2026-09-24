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
		// NOTA (ronda 2): EF Core 7.0.9 no permite nombrar la PK por tabla en una
		// jerarquía TPT (HasKey/HasName sólo puede llamarse una vez, en la raíz, y
		// aplica el mismo nombre a las 3 tablas -> "PK_PERSONA" duplicada en SQL
		// Server). No hay overload de HasName/SetName con StoreObjectIdentifier
		// para Key en esta versión (validado por reflection sobre
		// RelationalKeyBuilderExtensions/RelationalKeyExtensions). Fallback del
		// plan: se deja sin HasName y EF genera un nombre por tabla basado en
		// convención (PK_persona / PK_alumno / PK_docente).
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			   .HasColumnName("persona_id")
			   .ValueGeneratedNever();

		#region Información Personal
		builder.OwnsOne(p => p.DatosPersonales, informacionPersonalBuilder =>
		{
			informacionPersonalBuilder.Property(x => x.Apellido)
									  .HasColumnName("apellido")
									  .HasMaxLength(50)
									  .IsUnicode(false)
									  .IsRequired();

			informacionPersonalBuilder.Property(x => x.Nombre)
									  .HasColumnName("nombre")
									  .HasMaxLength(50)
									  .IsUnicode(false)
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
									  .HasMaxLength(20)
									  .IsUnicode(false)
									  .IsRequired();

			informacionPersonalBuilder.HasIndex(x => x.Documento)
									  .IsUnique();
		});
		#endregion

		#region Contacto
		builder.Property(x => x.Telefono)
			   .HasColumnName("telefono")
			   .HasMaxLength(15)
			   .IsUnicode(false);

		builder.Property(x => x.Email)
			   .HasColumnName("email")
			   .HasMaxLength(50)
			   .IsUnicode(false);

		builder.HasIndex(x => x.Email)
			   .IsUnique();
		#endregion
	}

	private void ConfigureTableDomicilios(EntityTypeBuilder<Persona> builder)
	{
		builder.OwnsOne(x => x.Domicilio, domiciliosBuilder =>
		{
			domiciliosBuilder.ToTable("domicilio");

			// FK_DOMICILIO_PERSONA
			domiciliosBuilder.WithOwner()
							 .HasForeignKey("persona_id")
							 .HasConstraintName("FK_DOMICILIO_PERSONA");

			// PK_DOMICILIO
			domiciliosBuilder.HasKey("persona_id")
							 .HasName("PK_DOMICILIO");

			#region Dirección
			domiciliosBuilder.OwnsOne(x => x.Direccion, direccionBuilder =>
			{
				direccionBuilder.Property(x => x.Calle)
								.HasColumnName("calle")
								.HasMaxLength(50)
								.IsUnicode(false)
								.IsRequired();

				direccionBuilder.Property(x => x.Altura)
								.HasColumnName("altura")
								.HasMaxLength(6)
								.IsUnicode(false)
								.IsRequired(false);

				direccionBuilder.Property(x => x.Vivienda)
								.HasColumnName("vivienda")
								.HasColumnType("varchar(20)")
								.HasConversion(toProvider => toProvider.ToString(),
											   fromProvider => (Vivienda) Enum.Parse(typeof(Vivienda), fromProvider))
								.IsRequired();

				direccionBuilder.Property(x => x.Observacion)
								.HasColumnName("observaciones")
								.HasMaxLength(120)
								.IsUnicode(false)
								.IsRequired(false);
			});
			#endregion

			#region Ubicación
			domiciliosBuilder.OwnsOne(x => x.Ubicacion, ubicacionBuilder =>
			{
				ubicacionBuilder.WithOwner();

				ubicacionBuilder.Property(x => x.Localidad)
								.HasColumnName("localidad")
								.HasMaxLength(50)
								.IsUnicode(false)
								.IsRequired();

				/**
				 * u.Property("CodigoPostal")
				 *  .HasColumnName("codigo_postal")
				 *  .HasColumnType("char(4)")
				 *  .IsRequired();
				 **/

				ubicacionBuilder.Property(x => x.Provincia)
								.HasColumnName("provincia")
								.HasMaxLength(50)
								.IsUnicode(false)
								.IsRequired();

				ubicacionBuilder.Property(x => x.Pais)
								.HasColumnName("pais")
								.HasMaxLength(50)
								.IsUnicode(false)
								.IsRequired();
			});
			#endregion
		});
	}
}