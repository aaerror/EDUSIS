using Domain.Shared;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Shared;

/// <summary>
/// Medición de <see cref="RangoFechas"/>: <c>Duracion</c> sobre el rango completo y
/// <c>TiempoTranscurrido</c> hasta hoy, con el reloj fijado para que el resultado no dependa de
/// la fecha de ejecución.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class RangoFechasTests
{
	#region Duracion
	[Fact]
	public void Duracion_en_dias_es_la_diferencia_entre_inicio_y_fin()
	{
		var rango = RangoFechas.Create(new DateTime(2026, 3, 1), new DateTime(2026, 3, 5));

		rango.Duracion(Tiempo.Dia).ShouldBe(4);
	}

	[Fact]
	public void Duracion_en_meses_cuenta_meses_calendario_completos()
	{
		var rango = RangoFechas.Create(new DateTime(2026, 1, 15), new DateTime(2026, 3, 15));

		rango.Duracion(Tiempo.Mes).ShouldBe(2);
	}

	[Fact]
	public void Duracion_en_meses_expresa_el_resto_como_fraccion_del_mes()
	{
		// Del 01/04 al 16/04 son 15 de los 30 días de abril.
		var rango = RangoFechas.Create(new DateTime(2026, 4, 1), new DateTime(2026, 4, 16));

		rango.Duracion(Tiempo.Mes).ShouldBe(0.5, tolerance: 1e-9);
	}

	[Fact]
	public void Duracion_en_meses_desde_fin_de_mes_respeta_el_ajuste_de_AddMonths()
	{
		// 31/01 + 1 mes = 28/02: el rango es un mes exacto aunque el día no coincida.
		var rango = RangoFechas.Create(new DateTime(2026, 1, 31), new DateTime(2026, 2, 28));

		rango.Duracion(Tiempo.Mes).ShouldBe(1);
	}

	[Fact]
	public void Duracion_en_años_atraviesa_un_bisiesto_sin_desfase()
	{
		var rango = RangoFechas.Create(new DateTime(2023, 6, 1), new DateTime(2025, 6, 1));

		rango.Duracion(Tiempo.Año).ShouldBe(2);
	}

	[Fact]
	public void Duracion_de_un_rango_indeterminado_es_cero()
	{
		var rango = RangoFechas.Create(new DateTime(2026, 3, 1));

		rango.Duracion(Tiempo.Dia).ShouldBe(0);
	}
	#endregion

	#region TiempoTranscurrido
	[Fact]
	public void TiempoTranscurrido_de_un_rango_que_no_inicio_es_cero()
	{
		using var _ = RelojFijo.En(new DateTime(2026, 3, 1));
		var rango = RangoFechas.Create(new DateTime(2026, 4, 1), new DateTime(2026, 5, 1));

		rango.TiempoTranscurrido(Tiempo.Dia).ShouldBe(0);
	}

	[Fact]
	public void TiempoTranscurrido_de_un_rango_en_curso_mide_hasta_hoy()
	{
		using var _ = RelojFijo.En(new DateTime(2026, 3, 11));
		var rango = RangoFechas.Create(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

		rango.TiempoTranscurrido(Tiempo.Dia).ShouldBe(10);
	}

	[Fact]
	public void TiempoTranscurrido_de_un_rango_indeterminado_mide_hasta_hoy()
	{
		using var _ = RelojFijo.En(new DateTime(2026, 3, 11));
		var rango = RangoFechas.Create(new DateTime(2026, 3, 1));

		rango.TiempoTranscurrido(Tiempo.Dia).ShouldBe(10);
	}

	[Fact]
	public void TiempoTranscurrido_de_un_rango_finalizado_se_detiene_en_la_fecha_de_fin()
	{
		using var _ = RelojFijo.En(new DateTime(2026, 6, 1));
		var rango = RangoFechas.Create(new DateTime(2026, 3, 1), new DateTime(2026, 3, 5));

		rango.TiempoTranscurrido(Tiempo.Dia).ShouldBe(4);
	}
	#endregion
}
