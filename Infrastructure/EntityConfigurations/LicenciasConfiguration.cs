using Domain.Licencias;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class LicenciasConfiguration : IEntityTypeConfiguration<Licencia>
{
	public void Configure(EntityTypeBuilder<Licencia> builder)
	{
		builder.ToTable("licencia");

		builder.Property(x => x.Id)
			   .HasColumnName("licencia_id")
			   .ValueGeneratedNever();

		// FK_DOCENTE_LICENCIA
		/*builder.HasOne<Docente>()
			   .WithMany()
			   .HasPrincipalKey()
			   .HasForeignKey(x => x.DocenteID)
			   .HasConstraintName("FK_DOCENTE_LICENCIA")
			   .IsRequired()*/;

		builder.Property(x => x.DocenteID)
			   .HasColumnName("docente_id");

		builder.HasKey(x => new { x.Id, x.DocenteID })
			   .HasName("PK_LICENCIA");

		builder.Property(x => x.Articulo)
			   .HasColumnName("articulo")
			   .HasColumnType("varchar(15)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (Articulo) Enum.Parse(typeof(Articulo), fromProvider))
			   .IsRequired();

		builder.Property(x => x.Estado)
			   .HasColumnName("estado")
			   .HasColumnType("varchar(15)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (Estado) Enum.Parse(typeof(Estado), fromProvider));

		builder.OwnsOne(x => x.Periodo, periodoBuilder =>
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

		builder.Property(x => x.Observacion)
			   .HasColumnName("observacion")
			   .HasColumnType("varchar(10)")
			   .HasMaxLength(250);
	}
}