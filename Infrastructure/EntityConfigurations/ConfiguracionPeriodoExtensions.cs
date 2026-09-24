using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal static class ConfiguracionPeriodoExtensions
{
	public static void ConfigurarPeriodo<TEntity>(
		this OwnedNavigationBuilder<TEntity, RangoFechas> periodoBuilder,
		string columnaInicio = "fecha_inicio",
		string columnaFin = "fecha_fin")
		where TEntity : class
	{
		periodoBuilder.Property(x => x.FechaInicio)
					  .HasColumnName(columnaInicio)
					  .HasColumnType("date")
					  .IsRequired();

		periodoBuilder.Property(x => x.FechaFin)
					  .HasColumnName(columnaFin)
					  .HasColumnType("date")
					  .IsRequired(false);
	}
}
