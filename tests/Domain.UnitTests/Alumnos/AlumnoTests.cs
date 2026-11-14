using Domain.Alumnos.DomainEvents;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Alumnos;

/// <summary>
/// Agregado <see cref="Domain.Alumnos.Alumno"/>: alta (con el evento <c>AlumnoInscriptoDomainEvent</c>
/// encolado), consulta de estado y baja institucional (<c>AlumnoDesinscriptoDomainEvent</c>).
/// </summary>
/// <remarks>
/// La regla de edad del alumno NO vive acá: es relacional contra el grado al que se inscribe y la
/// gobierna <c>Domain.Cursos.EdadEscolar</c> (ver <c>EdadEscolarTests</c>). Hasta el 2026-11-12
/// esta clase tenía un placeholder vacío con <c>Skip</c> por H-004 que afirmaba una excepción
/// absoluta, <c>AlumnoMayorDeEdadException</c>, borrada al dirimir la fila: la mayoría de edad ya
/// era una consulta (<c>DatosPersonales.EsMayorDeEdad()</c>) y no una invariante.
/// </remarks>
[Trait("Categoria", Categorias.Unidad)]
public class AlumnoTests
{
	#region Alta
	[Fact]
	public void Un_alumno_nuevo_guarda_legajo_y_arranca_con_periodo_vigente()
	{
		var alumno = new AlumnoBuilder().ConLegajo("AL0001").Build();

		alumno.Legajo.ShouldBe("AL0001");
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

	[Fact]
	public void Un_alumno_con_legajo_de_formato_invalido_lanza_FormatException()
	{
		Should.Throw<FormatException>(() => new AlumnoBuilder().ConLegajo("A-000123").Build());
	}
	#endregion

	#region Legajo
	[Fact]
	public void SiguienteLegajo_sin_legajos_previos_devuelve_el_primero()
	{
		Domain.Alumnos.Alumno.SiguienteLegajo(null).ShouldBe("AL0001");
	}

	[Fact]
	public void SiguienteLegajo_incrementa_en_uno_los_cuatro_digitos_del_ultimo_legajo()
	{
		Domain.Alumnos.Alumno.SiguienteLegajo("AL0009").ShouldBe("AL0010");
	}

	[Fact]
	public void SiguienteLegajo_pasado_el_ultimo_legajo_posible_lanza_LegajosAgotadosException()
	{
		var ex = Should.Throw<Exception>(() => Domain.Alumnos.Alumno.SiguienteLegajo("AL9999"));

		ex.GetType().Name.ShouldBe("LegajosAgotadosException");
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
	public void Desinscribir_deja_al_alumno_inactivo_el_mismo_dia()
	{
		var alumno = new AlumnoBuilder().Build();

		alumno.Desinscribir();

		alumno.EstaActivo().ShouldBeFalse();
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
	public void Desinscribir_a_un_alumno_ya_inactivo_lanza_AlumnoInactivoException()
	{
		var alumno = new AlumnoBuilder().Build();
		alumno.Desinscribir();
		alumno.LiberarEventos();

		var ex = Should.Throw<Exception>(() => alumno.Desinscribir());

		ex.GetType().Name.ShouldBe("AlumnoInactivoException");
		alumno.Eventos.ShouldBeEmpty();
	}
	#endregion
}
