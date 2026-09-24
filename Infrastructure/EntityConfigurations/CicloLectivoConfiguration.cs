using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EntityConfigurations;

// El catálogo existe sólo acá: CicloLectivo es un ValueObject de Cursante
// (Domain/Cursantes/CicloLectivo.cs), no una entidad de dominio propia. Se mapea como
// shared-type entity para poder restringir por FK el período de Cursante.CicloLectivo.Periodo
// sin crear una clase de dominio que no le pertenece a ningún agregado.
internal static class CicloLectivoConfiguration
{
	// No puede llamarse "CicloLectivo": ese nombre de entity type ya lo toma el tipo owned que
	// crea Cursante.OwnsOne(x => x.CicloLectivo) (mismo nombre de CLR type), y un shared-type
	// entity comparte el mismo espacio de nombres de entity types que el resto del modelo. EF
	// rechaza el modelo con "cannot be added because the model already contains an entity type
	// with the same name, but with a different CLR type" si se usa "CicloLectivo" acá.
	// Validado en un proyecto aislado antes de fijar el nombre (ver reporte del agente).
	public const string CATALOGO = "CicloLectivoCatalogo";

	public static ModelBuilder ConfigurarCatalogoCicloLectivo(this ModelBuilder modelBuilder)
	{
		modelBuilder.SharedTypeEntity<Dictionary<string, object>>(CATALOGO, builder =>
		{
			builder.ToTable("ciclo_lectivo");

			builder.IndexerProperty<string>("periodo")
				   .HasColumnName("periodo")
				   .HasColumnType("smallint")
				   .HasConversion(
						toProvider => short.Parse(toProvider),
						fromProvider => fromProvider.ToString());

			// PK_CICLO_LECTIVO
			builder.HasKey("periodo")
				   .HasName("PK_CICLO_LECTIVO");

			builder.HasData(
				new Dictionary<string, object> { ["periodo"] = "2020" },
				new Dictionary<string, object> { ["periodo"] = "2021" },
				new Dictionary<string, object> { ["periodo"] = "2022" },
				new Dictionary<string, object> { ["periodo"] = "2023" },
				new Dictionary<string, object> { ["periodo"] = "2024" },
				new Dictionary<string, object> { ["periodo"] = "2025" },
				new Dictionary<string, object> { ["periodo"] = "2026" },
				new Dictionary<string, object> { ["periodo"] = "2027" },
				new Dictionary<string, object> { ["periodo"] = "2028" },
				new Dictionary<string, object> { ["periodo"] = "2029" },
				new Dictionary<string, object> { ["periodo"] = "2030" },
				new Dictionary<string, object> { ["periodo"] = "2031" },
				new Dictionary<string, object> { ["periodo"] = "2032" },
				new Dictionary<string, object> { ["periodo"] = "2033" },
				new Dictionary<string, object> { ["periodo"] = "2034" },
				new Dictionary<string, object> { ["periodo"] = "2035" });
		});

		return modelBuilder;
	}
}

/*
// Versión anterior: mapeaba CicloLectivo como si fuera una entidad de dominio propia
// (HasOne<CicloLectivo> desde Cursante), lo que no corresponde porque CicloLectivo es un
// ValueObject. Reemplazada por el catálogo shared-type de arriba.
internal class CicloLectivoConfiguration : IEntityTypeConfiguration<CicloLectivo>
{
	public void Configure(EntityTypeBuilder<CicloLectivo> builder)
	{
		builder.ToTable("ciclo_lectivo");

		builder.Property<int>("ciclo_lectivo_id");

		builder.HasKey("ciclo_lectivo_id")
			   .HasName("PK_CICLO-LECTIVO");

		builder.Property(x => x.Periodo)
			   .HasColumnName("periodo")
			   .HasColumnType("smallint")
			   .HasMaxLength(4)
			   .HasConversion(
					toProvider => Int32.Parse(toProvider),
					fromProvider => fromProvider.ToString());
	}
}
*/
