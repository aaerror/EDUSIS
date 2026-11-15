using Domain.Docentes.Exceptions;
using Domain.Docentes.DomainEvents;
using Domain.Docentes.Puestos;
using Domain.Personas;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Docentes;

/// <summary>
/// Agregado <see cref="Domain.Docentes.Docente"/>: alta con validación de edad, legajo y CUIL,
/// baja institucional (evento <c>DocenteDesafectadoDomainEvent</c>) y ciclo de vida de los
/// cargos docentes. Las excepciones del módulo son <c>internal</c>: se verifican por el nombre
/// del tipo. Las que ningún método cablea quedan con pruebas <c>Skip</c>.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class DocenteTests
{
	#region Alta
	[Fact]
	public void Un_docente_nuevo_guarda_legajo_cuil_y_arranca_activo()
	{
		var docente = new DocenteBuilder().ConLegajo("100200").ConCuil("20301112229").Build();

		docente.Legajo.ShouldBe("100200");
		docente.CUIL.ShouldBe("20301112229");
		docente.Activo.ShouldBeTrue();
		docente.Puestos.ShouldBeEmpty();
	}

	[Fact]
	public void Un_docente_menor_de_18_años_lanza_DocenteMenorDeEdadException()
	{
		var builder = new DocenteBuilder().ConDatosPersonales(new DatosPersonalesBuilder().ConEdad(17));

		var ex = Should.Throw<Exception>(() => builder.Build());

		ex.GetType().Name.ShouldBe("DocenteMenorDeEdadException");
	}

	[Theory]
	[InlineData(nameof(Sexo.Femenino), 61)]
	[InlineData(nameof(Sexo.Masculino), 66)]
	public void Un_docente_en_edad_jubilatoria_lanza_DocenteEnEdadJubilatoriaException(string sexo, int edad)
	{
		var builder = new DocenteBuilder()
			.ConDatosPersonales(new DatosPersonalesBuilder().ConSexo(Enum.Parse<Sexo>(sexo)).ConEdad(edad));

		var ex = Should.Throw<Exception>(() => builder.Build());

		ex.GetType().Name.ShouldBe("DocenteEnEdadJubilatoriaException");
	}

	[Fact]
	public void Una_fecha_de_alta_futura_lanza_FechaAltaDocenteFuturaException()
	{
		var builder = new DocenteBuilder().ConFechaAlta(DateTime.Today.AddDays(1));

		Should.Throw<FechaAltaDocenteFuturaException>(() => builder.Build());
	}

	[Theory]
	[InlineData("12345")]
	[InlineData("1002000")]
	[InlineData("ABCDEF")]
	public void Un_legajo_que_no_tiene_6_digitos_lanza_LegajoDocenteInvalidoException(string legajo)
	{
		var builder = new DocenteBuilder().ConLegajo(legajo);

		Should.Throw<LegajoDocenteInvalidoException>(() => builder.Build());
	}

	[Fact]
	public void Un_cuil_con_prefijo_invalido_lanza_CuilDocenteInvalidoException()
	{
		var builder = new DocenteBuilder().ConCuil("99301112229");

		Should.Throw<CuilDocenteInvalidoException>(() => builder.Build());
	}
	#endregion

	#region Baja institucional
	[Fact]
	public void Desafectar_cierra_el_periodo_y_encola_DocenteDesafectadoDomainEvent()
	{
		var docente = new DocenteBuilder().Build();
		docente.LiberarEventos();

		docente.Desafectar();

		docente.Periodo.FechaFin.ShouldBe(DateTime.Today);
		docente.Activo.ShouldBeFalse();
		var evento = docente.Eventos.ShouldHaveSingleItem().ShouldBeOfType<DocenteDesafectadoDomainEvent>();
		evento.DocenteID.ShouldBe(docente.Id);
	}

	[Fact]
	public void Desafectar_a_un_docente_ya_inactivo_lanza_DocenteInactivoException()
	{
		var docente = new DocenteBuilder().Build();
		docente.Desafectar();
		docente.LiberarEventos();

		var ex = Should.Throw<Exception>(() => docente.Desafectar());

		ex.GetType().Name.ShouldBe("DocenteInactivoException");
		docente.Eventos.ShouldBeEmpty();
	}
	#endregion

	#region Cargos docentes
	[Fact]
	public void AsignarCargoDocente_agrega_el_puesto_al_agregado()
	{
		var docente = new DocenteBuilder().Build();

		docente.AsignarCargoDocente(nameof(Posicion.Profesor), nameof(EstadoPuesto.Pendiente), DateTime.Today, null);

		var puesto = docente.Puestos.ShouldHaveSingleItem();
		puesto.Posicion.ShouldBe(Posicion.Profesor);
		puesto.Estado.ShouldBe(EstadoPuesto.Pendiente);
		puesto.DocenteID.ShouldBe(docente.Id);
	}

	[Fact]
	public void AsignarCargoDocente_con_una_posicion_ya_activa_lanza_PuestoDocenteAsignadoException()
	{
		var docente = new DocenteBuilder().ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Activo)).Build();

		var ex = Should.Throw<Exception>(() =>
			docente.AsignarCargoDocente(nameof(Posicion.Profesor), nameof(EstadoPuesto.Pendiente), DateTime.Today, null));

		ex.GetType().Name.ShouldBe("PuestoDocenteAsignadoException");
	}

	[Fact]
	public void AsignarCargoDocente_con_fecha_de_inicio_anterior_al_alta_lanza_InicioPuestoAnteriorAlAltaDocenteException()
	{
		var docente = new DocenteBuilder().ConFechaAlta(DateTime.Today.AddYears(-1)).Build();

		Should.Throw<InicioPuestoAnteriorAlAltaDocenteException>(() =>
			docente.AsignarCargoDocente(nameof(Posicion.Profesor), nameof(EstadoPuesto.Pendiente), DateTime.Today.AddYears(-2), null));
	}

	[Fact]
	public void RescindirCargoDocente_con_un_puesto_inexistente_lanza_PuestoDocenteNoEncontradoException()
	{
		var docente = new DocenteBuilder().Build();

		var ex = Should.Throw<Exception>(() => docente.RescindirCargoDocente(Guid.NewGuid(), null));

		ex.GetType().Name.ShouldBe("PuestoDocenteNoEncontradoException");
	}

	[Fact]
	public void CambiarEventualidadCargoDocente_con_un_puesto_inexistente_lanza_PuestoDocenteNoEncontradoException()
	{
		var docente = new DocenteBuilder().Build();

		var ex = Should.Throw<Exception>(() => docente.CambiarEventualidadCargoDocente(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("PuestoDocenteNoEncontradoException");
	}

	[Fact]
	public void Rescindir_dos_veces_el_mismo_cargo_lanza_PuestoDocenteSinAsignarException()
	{
		var docente = new DocenteBuilder().ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Activo)).Build();
		var puestoID = docente.Puestos.ShouldHaveSingleItem().Id;
		docente.RescindirCargoDocente(puestoID, null);

		var ex = Should.Throw<Exception>(() => docente.RescindirCargoDocente(puestoID, null));

		ex.GetType().Name.ShouldBe("PuestoDocenteSinAsignarException");
	}

	[Fact]
	public void EliminarCargoDocente_quita_un_puesto_pendiente()
	{
		var docente = new DocenteBuilder().ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Pendiente)).Build();
		var puestoID = docente.Puestos.ShouldHaveSingleItem().Id;

		docente.EliminarCargoDocente(puestoID);

		docente.Puestos.ShouldBeEmpty();
	}

	[Fact]
	public void EliminarCargoDocente_de_un_puesto_no_pendiente_lanza_PuestoDocenteNoPendienteException()
	{
		var docente = new DocenteBuilder().ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Activo)).Build();
		var puestoID = docente.Puestos.ShouldHaveSingleItem().Id;

		Should.Throw<PuestoDocenteNoPendienteException>(() => docente.EliminarCargoDocente(puestoID));
	}

	[Fact]
	public void ModificarCargoDocente_de_un_puesto_no_pendiente_lanza_PuestoDocenteNoPendienteException()
	{
		var docente = new DocenteBuilder().ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Activo)).Build();
		var puestoID = docente.Puestos.ShouldHaveSingleItem().Id;

		Should.Throw<PuestoDocenteNoPendienteException>(() =>
			docente.ModificarCargoDocente(puestoID, nameof(Posicion.Preceptor), DateTime.Today, null));
	}

	/// <remarks>
	/// Regresión: <c>AsignarCargoDocente</c> hacía <c>Enum.Parse</c> antes de construir el puesto,
	/// así que una posición vacía moría con el <c>ArgumentException</c> del BCL (mensaje en
	/// inglés) y la guarda de <c>Puesto</c> nunca se alcanzaba.
	/// </remarks>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void AsignarCargoDocente_sin_posicion_lanza_PosicionPuestoRequeridaException(string posicion)
	{
		var docente = new DocenteBuilder().Build();

		Should.Throw<PosicionPuestoRequeridaException>(() =>
			docente.AsignarCargoDocente(posicion, nameof(EstadoPuesto.Pendiente), DateTime.Today, null));
	}

	[Fact]
	public void ModificarCargoDocente_sin_posicion_lanza_PosicionPuestoRequeridaException()
	{
		var docente = new DocenteBuilder().ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Pendiente)).Build();
		var puestoID = docente.Puestos.ShouldHaveSingleItem().Id;

		Should.Throw<PosicionPuestoRequeridaException>(() =>
			docente.ModificarCargoDocente(puestoID, " ", DateTime.Today, null));
	}

	[Fact]
	public void ModificarCargoDocente_con_un_puesto_inexistente_lanza_PuestoDocenteNoEncontradoException()
	{
		var docente = new DocenteBuilder().Build();

		Should.Throw<PuestoDocenteNoEncontradoException>(() =>
			docente.ModificarCargoDocente(Guid.NewGuid(), nameof(Posicion.Preceptor), DateTime.Today, null));
	}
	#endregion

	#region Confirmación en el cargo
	/// <remarks>
	/// La regla de <c>Puesto.EstablecerComoPuestoFijo</c> se afirma contra la excepción de dominio
	/// y no contra un <c>ArgumentException</c> del BCL. No existe una excepción
	/// <c>DocenteSinCargoDocenteException</c>, por redundante: el módulo ya
	/// distingue los tres casos con <c>PuestoDocenteNoEncontradoException</c>,
	/// <c>PuestoDocenteSinAsignarException</c> y <c>PuestoDocenteAsignadoException</c>, y ningún
	/// caso de uso necesitaba un cuarto tipo para "el docente no tiene ningún cargo".
	/// </remarks>
	[Fact]
	public void Confirmar_un_puesto_ya_indeterminado_lanza_DocenteConfirmadoEnCargoDocenteException()
	{
		var docente = new PuestoBuilder().BuildDocente();
		var puesto = docente.Puestos.Single();

		Should.Throw<DocenteConfirmadoEnCargoDocenteException>(
			() => docente.CambiarEventualidadCargoDocente(puesto.Id));
	}

	/// <remarks>
	/// <para>
	/// <strong>El puesto no activo es el que todavía no arrancó, no el que terminó</strong>, y es
	/// la única forma de alcanzar esta guarda. `Puesto.EstablecerPeriodo` rechaza toda fecha de
	/// inicio anterior a hoy y `RangoFechas` exige fin >= inicio, así que `FechaFin` nunca queda
	/// antes de hoy y un puesto recién construido no puede estar finalizado. Rescindir tampoco
	/// sirve: `Rescindir(fechaFin)` pasa por el mismo `RangoFechas`.
	/// </para>
	/// </remarks>
	[Fact]
	public void Confirmar_un_puesto_que_todavia_no_inicio_lanza_PuestoDocenteInactivoException()
	{
		var docente = new PuestoBuilder()
			.ConDesde(DateTime.Today.AddDays(1))
			.ConHasta(DateTime.Today.AddMonths(6))
			.BuildDocente();
		var puesto = docente.Puestos.Single();
		puesto.EstaActivo().ShouldBeFalse();

		Should.Throw<PuestoDocenteInactivoException>(
			() => docente.CambiarEventualidadCargoDocente(puesto.Id));
	}

	/// <remarks>
	/// Regresión de un defecto latente que se destapó al cablear la excepción de dominio. La guarda de
	/// puesto no activo interpolaba `Periodo.FechaFin.Value` para informar la fecha de
	/// finalización, dando por hecho que existe. Un puesto por tiempo indeterminado que todavía no
	/// inició la tiene en nulo, así que la guarda moría con `InvalidOperationException` —"Nullable
	/// object must have a value"— en vez de con la excepción de negocio. Nadie lo había visto
	/// porque el camino exige las dos condiciones a la vez y ninguna prueba las combinaba.
	/// </remarks>
	[Fact]
	public void Confirmar_un_puesto_indeterminado_que_no_inicio_informa_el_negocio_y_no_un_nulo()
	{
		var docente = new PuestoBuilder()
			.ConDesde(DateTime.Today.AddDays(1))
			.ConHasta(null)
			.BuildDocente();
		var puesto = docente.Puestos.Single();
		puesto.EstaActivo().ShouldBeFalse();
		puesto.EsEventual.ShouldBeFalse();

		var ex = Should.Throw<PuestoDocenteInactivoException>(
			() => docente.CambiarEventualidadCargoDocente(puesto.Id));

		ex.Message.ShouldContain("todavía no inició");
	}
	#endregion
}
