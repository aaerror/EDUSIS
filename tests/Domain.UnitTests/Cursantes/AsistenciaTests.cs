using Domain.Cursantes.Asistencias;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursantes;

/// <summary>
/// Value object <see cref="Asistencia"/> y el enumerado <see cref="Falta"/>: factorías por tipo
/// de falta, normalización de la fecha e igualdad estructural.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class AsistenciaTests
{
	#region Factorías
	[Fact]
	public void Ausencia_produce_una_falta_de_tipo_Ausencia_sin_minutos()
	{
		var asistencia = Asistencia.Ausencia(new DateTime(2026, 3, 10, 9, 30, 0), "Sin aviso");

		asistencia.Falta.ShouldBe(Falta.Ausencia);
		asistencia.Minutos.ShouldBeNull();
		asistencia.Fecha.ShouldBe(new DateTime(2026, 3, 10));
		asistencia.Observacion.ShouldBe("Sin aviso");
	}

	[Fact]
	public void Inasistencia_produce_una_falta_de_tipo_Inasistencia()
	{
		Asistencia.Inasistencia(new DateTime(2026, 3, 10), "Con aviso").Falta.ShouldBe(Falta.Inasistencia);
	}

	[Fact]
	public void Tardanza_conserva_los_minutos_de_demora()
	{
		var asistencia = Asistencia.Tardanza(new DateTime(2026, 3, 10), TimeSpan.FromMinutes(20), "Colectivo");

		asistencia.Falta.ShouldBe(Falta.Tardanza);
		asistencia.Minutos.ShouldBe(TimeSpan.FromMinutes(20));
	}
	#endregion

	#region Igualdad estructural
	[Fact]
	public void Dos_asistencias_con_los_mismos_componentes_son_iguales()
	{
		var fecha = new DateTime(2026, 3, 10);

		var una = Asistencia.Tardanza(fecha, TimeSpan.FromMinutes(10), "obs");
		var otra = Asistencia.Tardanza(fecha, TimeSpan.FromMinutes(10), "obs");

		una.Equals(otra).ShouldBeTrue();
		una.GetHashCode().ShouldBe(otra.GetHashCode());
	}

	[Fact]
	public void Cambiar_el_tipo_de_falta_rompe_la_igualdad()
	{
		var fecha = new DateTime(2026, 3, 10);

		Asistencia.Ausencia(fecha, "obs").Equals(Asistencia.Inasistencia(fecha, "obs")).ShouldBeFalse();
	}
	#endregion
}
