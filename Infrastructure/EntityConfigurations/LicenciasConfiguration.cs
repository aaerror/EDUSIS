using Domain.Docentes;
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

		builder.HasKey(x => x.Id)
			   .HasName("PK_LICENCIA");

		builder.Property(x => x.DocenteID)
			   .HasColumnName("docente_id");

		// FK_LICENCIA_DOCENTE
		builder.HasOne<Docente>()
			   .WithMany()
			   .HasForeignKey(x => x.DocenteID)
			   .HasConstraintName("FK_LICENCIA_DOCENTE")
			   .OnDelete(DeleteBehavior.Restrict);

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

		builder.OwnsOne(x => x.Periodo, periodoBuilder => periodoBuilder.ConfigurarPeriodo());

		builder.Property(x => x.Observacion)
			   .HasColumnName("observacion")
			   .HasMaxLength(250)
			   .IsUnicode(false);
	}
}