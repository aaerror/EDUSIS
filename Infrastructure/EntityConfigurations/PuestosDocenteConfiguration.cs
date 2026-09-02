using Domain.Docentes.Puestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class PuestosDocenteConfiguration : IEntityTypeConfiguration<Puesto>
{
	public void Configure(EntityTypeBuilder<Puesto> builder)
	{
		builder.ToTable("puesto");

		builder.Property(x => x.Id)
			   .HasColumnName("puesto_id")
			   .ValueGeneratedNever();

		builder.HasKey(x => x.Id)
			   .HasName("PK_PUESTO");

		builder.Property(x => x.DocenteID)
			   .HasColumnName("docente_id");

		builder.Property(x => x.Estado)
			   .HasColumnName("estado")
			   .HasColumnType("varchar(15)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (EstadoPuesto) Enum.Parse(typeof(EstadoPuesto), fromProvider));

		builder.Property(x => x.Posicion)
			   .HasColumnName("posicion")
			   .HasColumnType("varchar(15)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (Posicion) Enum.Parse(typeof(Posicion), fromProvider));

		builder.Property(x => x.EsEventual)
			   .HasColumnName("es_eventual")
			   .HasColumnType("bit");

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
	}
}
