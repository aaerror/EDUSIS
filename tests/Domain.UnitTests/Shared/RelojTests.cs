using Domain.Docentes.Exceptions;
using Domain.Docentes.Puestos;
using Domain.Shared;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Shared;

/// <summary>
/// Contrato de <see cref="Reloj"/>: por defecto el reloj del sistema, reemplazable por flujo de
/// ejecución y restaurable. Incluye reglas de vigencia que sólo se pueden ejercitar con la fecha
/// fijada (antes las pruebas recurrían a rodeos de "el mismo día").
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class RelojTests
{
	private static readonly DateTime _unLunes = new(2026, 3, 2);

	#region Contrato
	[Fact]
	public void Sin_reloj_fijado_Hoy_es_la_fecha_del_sistema()
	{
		Reloj.Hoy.ShouldBe(DateTime.Today);
	}

	[Fact]
	public void Fijar_reemplaza_la_fecha_y_liberarlo_la_restaura()
	{
		using (RelojFijo.En(_unLunes))
		{
			Reloj.Hoy.ShouldBe(_unLunes);
		}

		Reloj.Hoy.ShouldBe(DateTime.Today);
	}

	[Fact]
	public void Un_reloj_fijado_anidado_restaura_el_exterior_al_liberarse()
	{
		using var exterior = RelojFijo.En(_unLunes);

		using (RelojFijo.En(_unLunes.AddDays(10)))
		{
			Reloj.Hoy.ShouldBe(_unLunes.AddDays(10));
		}

		Reloj.Hoy.ShouldBe(_unLunes);
	}

	[Fact]
	public void La_fecha_fijada_descarta_la_hora()
	{
		using var _ = RelojFijo.En(_unLunes.AddHours(15));

		Reloj.Hoy.ShouldBe(_unLunes);
	}

	/// <remarks>
	/// Lo que justifica el <c>AsyncLocal</c>: con un estático compartido, dos pruebas en paralelo
	/// se pisarían la fecha.
	/// </remarks>
	[Fact]
	public async Task Dos_flujos_en_paralelo_no_ven_la_fecha_del_otro()
	{
		using var listos = new Barrier(2);

		DateTime Observar(DateTime fecha)
		{
			using var _ = RelojFijo.En(fecha);
			// Ambos flujos tienen su fecha fijada a la vez antes de leerla.
			listos.SignalAndWait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
			return Reloj.Hoy;
		}

		var uno = Task.Run(() => Observar(_unLunes));
		var otro = Task.Run(() => Observar(_unLunes.AddYears(1)));

		(await uno).ShouldBe(_unLunes);
		(await otro).ShouldBe(_unLunes.AddYears(1));
	}
	#endregion

	#region Vigencia con fecha fijada
	[Fact]
	public void Un_rango_finaliza_el_dia_siguiente_a_su_fecha_de_fin()
	{
		var rango = RangoFechas.Create(_unLunes, _unLunes.AddDays(4));

		using (RelojFijo.En(_unLunes.AddDays(4)))
		{
			rango.HaFinalizado().ShouldBeFalse();
			rango.EstaVigente().ShouldBeTrue();
		}

		using (RelojFijo.En(_unLunes.AddDays(5)))
		{
			rango.HaFinalizado().ShouldBeTrue();
			rango.EstaVigente().ShouldBeFalse();
		}
	}

	/// <remarks>
	/// El caso que motivó la guarda de vigencia de <c>Puesto.Rescindir</c>: sin ella, rescindir
	/// sin fecha un eventual vencido le corría la fecha de fin a hoy.
	/// </remarks>
	[Fact]
	public void Rescindir_sin_fecha_un_puesto_eventual_vencido_lanza_PuestoDocenteInactivoException_y_no_lo_extiende()
	{
		var fin = _unLunes.AddDays(4);
		Domain.Docentes.Docente docente;
		using (RelojFijo.En(_unLunes))
		{
			docente = new DocenteBuilder()
				.ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Activo), _unLunes, fin)
				.Build();
		}
		var puesto = docente.Puestos.Single();

		using var _ = RelojFijo.En(fin.AddDays(10));

		Should.Throw<PuestoDocenteInactivoException>(() => docente.RescindirCargoDocente(puesto.Id, null));
		puesto.Periodo.FechaFin.ShouldBe(fin);
	}

	[Fact]
	public void Desafectar_un_docente_cierra_su_periodo_en_la_fecha_del_reloj()
	{
		using var _ = RelojFijo.En(_unLunes);
		var docente = new DocenteBuilder().Build();

		docente.Desafectar();

		docente.Periodo.FechaFin.ShouldBe(_unLunes);
	}
	#endregion
}
