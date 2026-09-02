using Domain.Alumnos.DomainEvents;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Alumnos;

/// <summary>
/// Agregado <see cref="Domain.Alumnos.Alumno"/>: alta (con el evento <c>AlumnoInscriptoDomainEvent</c>
/// encolado), consulta de estado y baja institucional (<c>AlumnoDesinscriptoDomainEvent</c>).
/// Dos excepciones del módulo quedan cubiertas con pruebas <c>Skip</c> (H-003, H-004).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class AlumnoTests
{
	#region Alta
	[Fact]
	public void Un_alumno_nuevo_guarda_legajo_y_arranca_con_periodo_vigente()
	{
		var alumno = new AlumnoBuilder().ConLegajo("A-000123").Build();

		alumno.Legajo.ShouldBe("A-000123");
		alumno.EstaActivo().ShouldBeTrue();
		alumno.Periodo.FechaInicio.ShouldBe(DateTime.Today);
		alumno.Periodo.FechaFin.ShouldBeNull();
	}

	[Fact]
	public void Un_alumno_nuevo_encola_el_evento_AlumnoInscriptoDomainEvent_con_su_Id()
	{
		var alumno = new AlumnoBuilder().Build();

		var evento = alumno.Eventos.ShouldHaveSingleItem().ShouldBeOfType<AlumnoInscriptoDomainEvent>();
		evento.AlumnoID.ShouldBe(alumno.Id);
	}
	#endregion

	#region Baja institucional
	[Fact]
	public void Desinscribir_cierra_el_periodo_con_la_fecha_de_hoy()
	{
		var alumno = new AlumnoBuilder().Build();

		alumno.Desinscribir();

		alumno.Periodo.FechaFin.ShouldBe(DateTime.Today);
	}

	[Fact]
	public void Hoy_un_alumno_desinscripto_sigue_reportando_EstaActivo_true_el_mismo_dia()
	{
		var alumno = new AlumnoBuilder().Build();

		alumno.Desinscribir();

		// Documenta el estado actual (H-003): EstaActivo() => Periodo.EstaVigente(), y
		// HaFinalizado() sólo es true con FechaFin estrictamente anterior a hoy. Recién mañana
		// el alumno figurará inactivo.
		alumno.EstaActivo().ShouldBeTrue();
	}

	[Fact]
	public void Desinscribir_encola_el_evento_AlumnoDesinscriptoDomainEvent()
	{
		var alumno = new AlumnoBuilder().Build();
		alumno.LiberarEventos();

		alumno.Desinscribir();

		var evento = alumno.Eventos.ShouldHaveSingleItem().ShouldBeOfType<AlumnoDesinscriptoDomainEvent>();
		evento.AlumnoID.ShouldBe(alumno.Id);
	}

	[Fact]
	public void Hoy_desinscribir_dos_veces_el_mismo_dia_no_falla_y_reencola_el_evento_de_baja()
	{
		var alumno = new AlumnoBuilder().Build();
		alumno.Desinscribir();
		alumno.LiberarEventos();

		// Documenta el estado actual (H-003): el guard de Desinscribir usa Periodo.HaFinalizado()
		// (estrictamente pasado), así que una segunda baja el mismo día no lanza
		// AlumnoInactivoException y vuelve a encolar AlumnoDesinscriptoDomainEvent.
		Should.NotThrow(() => alumno.Desinscribir());
		alumno.Eventos.ShouldHaveSingleItem().ShouldBeOfType<AlumnoDesinscriptoDomainEvent>();
	}
	#endregion

	#region Excepciones del módulo — defectos conocidos
	[Fact(Skip = "H-003: AlumnoInactivoException es inalcanzable; el guard de Desinscribir sólo dispara con FechaFin estrictamente anterior a hoy. Ver hallazgos.md.")]
	public void Desinscribir_a_un_alumno_ya_inactivo_lanza_AlumnoInactivoException()
	{
	}

	[Fact(Skip = "H-004: AlumnoMayorDeEdadException no se lanza desde ningún constructor ni método del dominio. Ver hallazgos.md.")]
	public void Un_alumno_que_alcanza_la_mayoria_de_edad_lanza_AlumnoMayorDeEdadException()
	{
	}
	#endregion
}
