using Domain.Catedras;
using Domain.Catedras.SituacionesRevista;
using Domain.Docentes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Shared;

namespace Infrastructure.EntityConfigurations;

internal class SituacionRevistaConfiguration : IEntityTypeConfiguration<SituacionRevista>
{
	public void Configure(EntityTypeBuilder<SituacionRevista> builder)
	{
		ConfigureTableSituacionRevista(builder);
	}

	private void ConfigureTableSituacionRevista(EntityTypeBuilder<SituacionRevista> builder)
	{
		builder.ToTable("situacion_revista");

		builder.Property(x => x.Id)
			   .HasColumnName("situacion_revista_id")
			   .ValueGeneratedNever();

		// PK_SITUACION_REVISTA
		builder.HasKey(x => x.Id)
			   .HasName("PK_SITUACION_REVISTA");

		// SHADOW PROPERTY: dueña de la relación con Catedra (FK_SITUACION_REVISTA_CATEDRA en CatedrasConfiguration).
		builder.Property<Guid>("CatedraID")
			   .HasColumnName("catedra_id");

		// AK_SITUACION_REVISTA_CATEDRA: sostiene la FK compuesta de Catedra.SituacionEnFuncionesID.
		builder.HasAlternateKey("CatedraID", nameof(SituacionRevista.Id))
			   .HasName("AK_SITUACION_REVISTA_CATEDRA");

		// FK_CATEDRA_SITUACION_REVISTA: FK compuesta (catedra_id, situacion_en_funciones_id) → clave alterna
		// (catedra_id, situacion_revista_id) de esta tabla, para garantizar en BD que la situación "en
		// funciones" pertenezca a la misma cátedra. Declarada acá (y no en CatedrasConfiguration) porque
		// depende de la propiedad sombra "CatedraID" y la clave alterna, definidas arriba en este archivo:
		// ApplyConfigurationsFromAssembly no garantiza el orden de aplicación de las configs.
		builder.HasOne<Catedra>()
			   .WithOne()
			   .HasForeignKey<Catedra>(nameof(Catedra.Id), nameof(Catedra.SituacionEnFuncionesID))
			   .HasPrincipalKey<SituacionRevista>("CatedraID", nameof(SituacionRevista.Id))
			   .OnDelete(DeleteBehavior.NoAction)
			   .HasConstraintName("FK_CATEDRA_SITUACION_REVISTA");

		builder.Property(x => x.DocenteID)
			   .HasColumnName("docente_id");

		// FK_SITUACION_REVISTA_DOCENTE
		builder.HasOne<Docente>()
			   .WithMany()
			   .HasForeignKey(x => x.DocenteID)
			   .OnDelete(DeleteBehavior.Restrict)
			   .HasConstraintName("FK_SITUACION_REVISTA_DOCENTE");

		builder.Property(x => x.Estado)
			   .HasColumnName("estado")
			   .HasColumnType("varchar(20)")
			   .HasConversion(toProvider => toProvider.Descripcion,
							  fromProvider => Enumeration.FromDescripcion<EstadoSituacionRevista>(fromProvider.ToString()));

		builder.Property(x => x.Cargo)
			   .HasColumnName("cargo")
			   .HasColumnType("varchar(10)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (Cargo) Enum.Parse(typeof(Cargo), fromProvider));

		builder.OwnsOne(x => x.Periodo, periodoBuilder => periodoBuilder.ConfigurarPeriodo());

		builder.Property(x => x.ReemplazaA)
			   .HasColumnName("reemplaza_a")
			   .IsRequired(false);

		// FK_SITUACION_REVISTA_REEMPLAZA_A: auto-referencial, cadena lineal de suplencias.
		builder.HasOne<SituacionRevista>()
			   .WithMany()
			   .HasForeignKey(x => x.ReemplazaA)
			   .OnDelete(DeleteBehavior.NoAction)
			   .HasConstraintName("FK_SITUACION_REVISTA_REEMPLAZA_A");
	}
}
