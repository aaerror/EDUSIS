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
/// del tipo. Las inalcanzables quedan con pruebas <c>Skip</c> (H-005, H-006, H-007).
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
	public void Una_fecha_de_alta_futura_lanza_ArgumentException()
	{
		var builder = new DocenteBuilder().ConFechaAlta(DateTime.Today.AddDays(1));

		Should.Throw<ArgumentException>(() => builder.Build());
	}

	[Theory]
	[InlineData("12345")]
	[InlineData("1002000")]
	[InlineData("ABCDEF")]
	public void Un_legajo_que_no_tiene_6_digitos_lanza_FormatException(string legajo)
	{
		var builder = new DocenteBuilder().ConLegajo(legajo);

		Should.Throw<FormatException>(() => builder.Build());
	}

	[Fact]
	public void Un_cuil_con_prefijo_invalido_lanza_FormatException()
	{
		var builder = new DocenteBuilder().ConCuil("99301112229");

		Should.Throw<FormatException>(() => builder.Build());
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
		var evento = docente.Eventos.ShouldHaveSingleItem().ShouldBeOfType<DocenteDesafectadoDomainEvent>();
		evento.DocenteID.ShouldBe(docente.Id);
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
	public void AsignarCargoDocente_con_fecha_de_inicio_anterior_al_alta_lanza_ArgumentException()
	{
		var docente = new DocenteBuilder().ConFechaAlta(DateTime.Today.AddYears(-1)).Build();

		Should.Throw<ArgumentException>(() =>
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
	public void EliminarCargoDocente_de_un_puesto_no_pendiente_lanza_ArgumentException()
	{
		var docente = new DocenteBuilder().ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Activo)).Build();
		var puestoID = docente.Puestos.ShouldHaveSingleItem().Id;

		Should.Throw<ArgumentException>(() => docente.EliminarCargoDocente(puestoID));
	}

	[Fact]
	public void ModificarCargoDocente_de_un_puesto_no_pendiente_lanza_ArgumentException()
	{
		var docente = new DocenteBuilder().ConPuesto(nameof(Posicion.Profesor), nameof(EstadoPuesto.Activo)).Build();
		var puestoID = docente.Puestos.ShouldHaveSingleItem().Id;

		Should.Throw<ArgumentException>(() =>
			docente.ModificarCargoDocente(puestoID, nameof(Posicion.Preceptor), DateTime.Today, null));
	}
	#endregion

	#region Excepciones inalcanzables / sin cablear
	[Fact(Skip = "H-005: DocenteInactivoException es inalcanzable en una prueba de un mismo día; el guard de Desafectar usa Periodo.HaFinalizado() y los chequeos !Activo requieren una abstracción de reloj. Ver hallazgos.md.")]
	public void Desafectar_a_un_docente_ya_inactivo_lanza_DocenteInactivoException()
	{
	}

	[Fact(Skip = "H-006: DocenteConfirmadoEnCargoDocenteException no se lanza desde ningún método del dominio. Ver hallazgos.md.")]
	public void Confirmar_un_docente_ya_confirmado_en_un_cargo_lanza_DocenteConfirmadoEnCargoDocenteException()
	{
	}

	[Fact(Skip = "H-007: DocenteSinCargoDocenteException no se lanza desde ningún método del dominio. Ver hallazgos.md.")]
	public void Operar_sobre_un_docente_sin_cargos_lanza_DocenteSinCargoDocenteException()
	{
	}
	#endregion
}
