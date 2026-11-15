using Domain.Docentes.Exceptions;
using System.Reflection;
using Domain.Docentes.Puestos;
using Domain.Shared;
using Domain.Shared.Exceptions;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Docentes;

/// <summary>
/// Entidad <see cref="Puesto"/> (cargo docente) y sus enumerados <see cref="EstadoPuesto"/> y
/// <see cref="Posicion"/>: alta, invariantes del período y de la posición, y ciclo de vida
/// (rescisión, paso a puesto fijo). El constructor y los mutadores son <c>internal</c> — sólo
/// <c>Domain.Docentes.Docente</c> instancia y modifica puestos —, así que
/// <see cref="PuestoBuilder"/> asigna el cargo sobre un <c>Docente</c> auxiliar.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class PuestoTests
{
	#region Alta
	[Fact]
	public void Un_puesto_nuevo_guarda_posicion_estado_y_periodo()
	{
		var puesto = new PuestoBuilder()
			.ConPosicion(nameof(Posicion.Preceptor))
			.ConEstado(nameof(EstadoPuesto.Pendiente))
			.ConDesde(DateTime.Today)
			.Build();

		puesto.Posicion.ShouldBe(Posicion.Preceptor);
		puesto.Estado.ShouldBe(EstadoPuesto.Pendiente);
		puesto.Periodo.FechaInicio.ShouldBe(DateTime.Today);
		puesto.EsEventual.ShouldBeFalse();
		puesto.EstaActivo().ShouldBeTrue();
	}

	[Fact]
	public void Un_puesto_con_fecha_de_fin_queda_marcado_como_eventual()
	{
		var puesto = new PuestoBuilder().ConHasta(DateTime.Today.AddMonths(6)).Build();

		puesto.EsEventual.ShouldBeTrue();
	}

	[Fact]
	public void Un_puesto_con_fecha_de_inicio_anterior_a_hoy_lanza_InicioPuestoPasadoException()
	{
		var builder = new PuestoBuilder().ConDesde(DateTime.Today.AddDays(-1));

		Should.Throw<InicioPuestoPasadoException>(() => builder.Build());
	}

	[Fact]
	public void Un_estado_de_puesto_fuera_del_enum_lanza_ArgumentException()
	{
		var builder = new PuestoBuilder().ConEstado("Vacaciones");

		Should.Throw<ArgumentException>(() => builder.Build());
	}

	/// <remarks>
	/// Inactivo sólo se alcanza rescindiendo: un alta que naciera inactiva o en suspenso salteaba
	/// las guardas de la rescisión.
	/// </remarks>
	[Theory]
	[InlineData(nameof(EstadoPuesto.Inactivo))]
	[InlineData(nameof(EstadoPuesto.EnSupenso))]
	public void Un_alta_con_estado_distinto_de_pendiente_o_activo_lanza_EstadoInicialPuestoInvalidoException(string estado)
	{
		var builder = new PuestoBuilder().ConEstado(estado);

		Should.Throw<EstadoInicialPuestoInvalidoException>(() => builder.Build());
	}

	[Fact]
	public void Una_posicion_fuera_del_enum_lanza_ArgumentException()
	{
		var builder = new PuestoBuilder().ConPosicion("Bibliotecario");

		Should.Throw<ArgumentException>(() => builder.Build());
	}
	#endregion

	#region Ciclo de vida
	// Los mutadores de Puesto son internal: el ciclo de vida se ejercita a través de la raíz Docente.
	[Fact]
	public void Rescindir_sin_fecha_pasa_el_puesto_a_inactivo_y_lo_cierra_hoy()
	{
		var docente = new PuestoBuilder().ConEstado(nameof(EstadoPuesto.Activo)).BuildDocente();
		var puesto = docente.Puestos.Single();

		docente.RescindirCargoDocente(puesto.Id, null);

		puesto.Estado.ShouldBe(EstadoPuesto.Inactivo);
		puesto.Periodo.FechaFin.ShouldBe(DateTime.Today);
	}

	[Fact]
	public void Rescindir_con_fecha_cierra_el_puesto_en_esa_fecha()
	{
		var docente = new PuestoBuilder().ConEstado(nameof(EstadoPuesto.Activo)).BuildDocente();
		var puesto = docente.Puestos.Single();
		var fin = DateTime.Today.AddDays(10);

		docente.RescindirCargoDocente(puesto.Id, fin);

		puesto.Estado.ShouldBe(EstadoPuesto.Inactivo);
		puesto.Periodo.FechaFin.ShouldBe(fin);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Rescindir_un_puesto_pendiente_lanza_RescisionPuestoNoActivoException_y_no_lo_toca(bool conFecha)
	{
		var docente = new PuestoBuilder().ConEstado(nameof(EstadoPuesto.Pendiente)).BuildDocente();
		var puesto = docente.Puestos.Single();
		DateTime? fin = conFecha ? DateTime.Today.AddDays(10) : null;

		Should.Throw<RescisionPuestoNoActivoException>(() => docente.RescindirCargoDocente(puesto.Id, fin));

		puesto.Estado.ShouldBe(EstadoPuesto.Pendiente);
		puesto.Periodo.FechaFin.ShouldBeNull();
	}

	/// <remarks>
	/// Antes, sin fecha, esta rescisión no hacía nada y no avisaba.
	/// </remarks>
	[Fact]
	public void Rescindir_un_puesto_activo_que_todavia_no_inicio_lanza_PuestoDocenteInactivoException()
	{
		var docente = new PuestoBuilder()
			.ConEstado(nameof(EstadoPuesto.Activo))
			.ConDesde(DateTime.Today.AddDays(5))
			.BuildDocente();
		var puesto = docente.Puestos.Single();

		Should.Throw<PuestoDocenteInactivoException>(() => docente.RescindirCargoDocente(puesto.Id, null));

		puesto.Estado.ShouldBe(EstadoPuesto.Activo);
	}

	[Fact]
	public void Rescindir_con_una_fecha_anterior_al_inicio_lanza_FechasInconsistentesException()
	{
		var docente = new PuestoBuilder()
			.ConEstado(nameof(EstadoPuesto.Activo))
			.BuildDocente();
		var puesto = docente.Puestos.Single();

		Should.Throw<FechasInconsistentesException>(() => docente.RescindirCargoDocente(puesto.Id, DateTime.Today.AddDays(-1)));
	}

	[Fact]
	public void EstablecerComoPuestoFijo_sobre_un_puesto_eventual_activo_lo_vuelve_indeterminado()
	{
		var docente = new PuestoBuilder().ConHasta(DateTime.Today.AddMonths(6)).BuildDocente();
		var puesto = docente.Puestos.Single();

		docente.CambiarEventualidadCargoDocente(puesto.Id);

		puesto.EsEventual.ShouldBeFalse();
		puesto.Periodo.FechaFin.ShouldBeNull();
	}

	/// <remarks>
	/// Regresión: confirmar reutilizaba la validación de alta ("no se carga un período que ya
	/// empezó"), así que un puesto eventual sólo podía confirmarse el mismo día en que iniciaba.
	/// El alta rechaza inicios pasados, de modo que el puesto ya iniciado se fabrica por reflexión
	/// sobre el setter privado de <c>Periodo</c>, como lo rehidrataría EF desde la base.
	/// </remarks>
	[Fact]
	public void EstablecerComoPuestoFijo_sobre_un_puesto_eventual_iniciado_antes_de_hoy_conserva_el_inicio()
	{
		var docente = new PuestoBuilder().ConHasta(DateTime.Today.AddMonths(6)).BuildDocente();
		var puesto = docente.Puestos.Single();
		var inicio = DateTime.Today.AddMonths(-2);
		typeof(Puesto).GetProperty(nameof(Puesto.Periodo))!
			.SetValue(puesto, RangoFechas.Create(inicio, DateTime.Today.AddMonths(4)));

		docente.CambiarEventualidadCargoDocente(puesto.Id);

		puesto.EsEventual.ShouldBeFalse();
		puesto.Periodo.FechaInicio.ShouldBe(inicio);
		puesto.Periodo.FechaFin.ShouldBeNull();
	}

	/// <remarks>
	/// La regla se reporta con una excepción de dominio y no con <c>ArgumentException</c> del BCL.
	/// <c>DocenteConfirmadoEnCargoDocenteException</c>
	/// NO deriva de <c>ArgumentException</c>, así que esta prueba habría quedado en rojo sin
	/// actualizarla — es la señal de que el cableado llegó a este camino y no sólo al archivo.
	/// </remarks>
	[Fact]
	public void EstablecerComoPuestoFijo_sobre_un_puesto_indeterminado_lanza_DocenteConfirmadoEnCargoDocente()
	{
		var docente = new PuestoBuilder().BuildDocente();
		var puesto = docente.Puestos.Single();

		Should.Throw<DocenteConfirmadoEnCargoDocenteException>(
			() => docente.CambiarEventualidadCargoDocente(puesto.Id));
	}

	[Fact]
	public void ActualizarPuestoDocente_cambia_posicion_y_periodo()
	{
		var docente = new PuestoBuilder().ConPosicion(nameof(Posicion.Profesor)).BuildDocente();
		var puesto = docente.Puestos.Single();

		docente.ModificarCargoDocente(puesto.Id, nameof(Posicion.Auxiliar), DateTime.Today, DateTime.Today.AddMonths(3));

		puesto.Posicion.ShouldBe(Posicion.Auxiliar);
		puesto.EsEventual.ShouldBeTrue();
	}

	[Fact]
	public void El_constructor_y_los_mutadores_de_Puesto_no_son_publicos()
	{
		var tipo = typeof(Puesto);
		var publicos = BindingFlags.Public | BindingFlags.Instance;

		tipo.GetConstructors(publicos).ShouldBeEmpty();
		tipo.GetMethod(nameof(Puesto.EstaActivo), publicos).ShouldNotBeNull();
		new[] { "ActualizarPuestoDocente", "EstablecerComoPuestoFijo", "Rescindir" }
			.ShouldAllBe(nombre => tipo.GetMethod(nombre, publicos) == null);
	}
	#endregion
}
