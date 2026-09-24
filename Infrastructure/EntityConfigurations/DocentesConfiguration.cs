using Domain.Docentes;
using Domain.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class DocentesConfiguration : IEntityTypeConfiguration<Docente>
{
	public void Configure(EntityTypeBuilder<Docente> builder)
	{
		ConfigureTableDocentes(builder);
		//ConfigureTableLicencias(builder);
		//ConfigureTablePuestos(builder);
	}

	private void ConfigureTableDocentes(EntityTypeBuilder<Docente> builder)
	{
		builder.HasBaseType(typeof(Persona));

		builder.ToTable("docente");

		// PK/FK de herencia TPT: ver nota en PersonasConfiguration.ConfigureTablePersonas.
		// No se puede nombrar la PK por tabla (HasKey no admite llamarse en un tipo
		// derivado: EF lanza InvalidOperationException) ni la FK de herencia
		// docente->persona (no existe como IMutableForeignKey durante Configure();
		// EF la crea recién al finalizar el modelo). Quedan con el nombre de EF
		// por convención (PK_docente / FK_docente_persona_persona_id).
		builder.Property(x => x.Legajo)
			   .HasColumnName("legajo")
			   .HasMaxLength(6)
			   .IsUnicode(false)
			   .IsRequired();

		builder.Property(x => x.CUIL)
			   .HasColumnName("cuil")
			   .HasMaxLength(11)
			   .IsUnicode(false)
			   .IsRequired();

		builder.OwnsOne(x => x.Periodo, periodoBuilder => periodoBuilder.ConfigurarPeriodo());

		builder.Property(x => x.Activo)
			   .HasColumnName("activo")
			   .HasColumnType("bit");

		// FK_PUESTO_DOCENTE
		builder.HasMany(x => x.Puestos)
			   .WithOne()
			   .HasForeignKey(x => x.DocenteID)
			   .HasConstraintName("FK_PUESTO_DOCENTE")
			   .OnDelete(DeleteBehavior.Cascade);

		//builder.Ignore(x => x.EstaActivo());
		builder.Ignore(x => x.Puesto);

		builder.Navigation(x => x.Puestos)
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}

	/*
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
	*/

	/*
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

			puestosBuilder.OwnsOne(x => x.Periodo, periodoBuilder =>
			{
				periodoBuilder.Property(a => a.FechaInicio)
							  .HasColumnName("fecha_inicio")
							  .HasColumnType("date")
							  .IsRequired();

				periodoBuilder.Property(a => a.FechaFin)
							  .HasColumnName("fecha_fin")
							  .HasColumnType("date")
							  .IsRequired(false);
			});
		})
			   .Navigation(nameof(Docente.Puestos))
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}
	*/
}
