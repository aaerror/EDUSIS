using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Domain.UnitTests.Cursantes;

/// <summary>
/// Agregado <see cref="Cursante"/>: alta con su división, alumno y ciclo lectivo, y registro de
/// calificaciones con métodos para crear, eliminar y modificar observaciones. <c>CalificacionNoEncontradaException</c>,
/// <c>SinDatosDivisionException</c> y <c>SinDatosAlumnoException</c> son <c>internal</c>: se
/// verifican por el nombre del tipo.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CursanteTests
{
	#region Alta
	[Fact]
	public void Un_cursante_nuevo_guarda_alumno_ciclo_lectivo_y_fecha_de_inicio()
	{
		var alumnoID = Guid.NewGuid();
		var ciclo = new CicloLectivoBuilder().ConAño(2026).Build();

		var cursante = new CursanteBuilder().ConAlumno(alumnoID).ConCicloLectivo(ciclo).ConFechaInicio(new DateTime(2026, 3, 1)).Build();

		cursante.AlumnoID.ShouldBe(alumnoID);
		cursante.CicloLectivo.ShouldBe(ciclo);
		cursante.FechaInicio.ShouldBe(new DateTime(2026, 3, 1));
		cursante.EsRecursante.ShouldBeFalse();
		cursante.Calificaciones.ShouldBeEmpty();
	}

	[Fact]
	public void Un_cursante_recursante_queda_marcado_como_tal()
	{
		var cursante = new CursanteBuilder().ComoRecursante().Build();

		cursante.EsRecursante.ShouldBeTrue();
	}

	[Fact]
	public void El_constructor_asigna_la_division_indicada()
	{
		// Regresión: el ctor recibía la división, la validaba y nunca la asignaba
		// (docs/plan/plan.md:98). Ya corregido en Domain; este test lo cubre para que no
		// vuelva a pasar desapercibido.
		var divisionID = Guid.NewGuid();

		var cursante = new CursanteBuilder().ConDivision(divisionID).Build();

		cursante.DivisionID.ShouldBe(divisionID);
	}

	[Fact]
	public void Un_cursante_sin_division_lanza_SinDatosDivisionException()
	{
		var ciclo = new CicloLectivoBuilder().Build();

		var ex = Should.Throw<Exception>(() => new Cursante(Guid.Empty, Guid.NewGuid(), ciclo, DateTime.Today));

		ex.GetType().Name.ShouldBe("SinDatosDivisionException");
	}

	[Fact]
	public void Un_cursante_sin_alumno_lanza_SinDatosAlumnoException()
	{
		var ciclo = new CicloLectivoBuilder().Build();

		var ex = Should.Throw<Exception>(() => new Cursante(Guid.NewGuid(), Guid.Empty, ciclo, DateTime.Today));

		ex.GetType().Name.ShouldBe("SinDatosAlumnoException");
	}

	[Fact]
	public void Un_cursante_sin_ciclo_lectivo_lanza_SinDatosAlumnoException_por_un_guard_compartido()
	{
		// HD-003 (docs/plan/hallazgos-tests-dominio.md): el guard del ctor funde dos
		// precondiciones distintas bajo una sola excepción. Un ciclo lectivo nulo debería
		// reportarse con una excepción propia (o al menos un guard separado), no como "sin
		// datos de alumno". Este test nombra la conducta actual, no la consagra como correcta.
		var ex = Should.Throw<Exception>(() => new Cursante(Guid.NewGuid(), Guid.NewGuid(), null!, DateTime.Today));

		ex.GetType().Name.ShouldBe("SinDatosAlumnoException");
	}
	#endregion

	#region Calificaciones
	[Fact]
	public void RegistrarCalificacion_agrega_una_calificacion_y_devuelve_su_id()
	{
		var cursante = new CursanteBuilder().Build();
		var materiaID = Guid.NewGuid();

		var id = cursante.RegistrarCalificacion(materiaID, DateTime.Today, Instancia.Parcial, 8, "Muy bien");

		var calificacion = cursante.Calificaciones.ShouldHaveSingleItem();
		calificacion.Id.ShouldBe(id);
		calificacion.MateriaID.ShouldBe(materiaID);
		calificacion.Nota.ShouldBe(8);
	}

	[Fact]
	public void RegistrarInasistenciaAExamen_agrega_una_inasistencia_y_devuelve_su_id()
	{
		var cursante = new CursanteBuilder().Build();
		var materiaID = Guid.NewGuid();

		var id = cursante.RegistrarInasistenciaAExamen(materiaID, DateTime.Today, Instancia.Parcial, "Enfermo");

		var calificacion = cursante.Calificaciones.ShouldHaveSingleItem();
		calificacion.Id.ShouldBe(id);
		calificacion.MateriaID.ShouldBe(materiaID);
		calificacion.Rindio.ShouldBeFalse();
	}

	[Fact]
	public void QuitarCalificacion_elimina_una_calificacion_existente()
	{
		var cursante = new CursanteBuilder().Build();
		var id = cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Instancia.Parcial, 8, null);

		cursante.QuitarCalificacion(id);

		cursante.Calificaciones.ShouldBeEmpty();
	}

	[Fact]
	public void QuitarCalificacion_con_datos_repetidos_elimina_solo_la_indicada()
	{
		var cursante = new CursanteBuilder().Build();
		var materiaID = Guid.NewGuid();
		var primera = cursante.RegistrarCalificacion(materiaID, DateTime.Today, Instancia.Parcial, 8, null);
		var segunda = cursante.RegistrarCalificacion(materiaID, DateTime.Today, Instancia.Parcial, 8, null);

		cursante.QuitarCalificacion(segunda);

		cursante.Calificaciones.ShouldHaveSingleItem().Id.ShouldBe(primera);
	}

	[Fact]
	public void QuitarCalificacion_inexistente_lanza_CalificacionNoEncontradaException()
	{
		var cursante = new CursanteBuilder().Build();

		var ex = Should.Throw<Exception>(() => cursante.QuitarCalificacion(Guid.NewGuid()));

		ex.GetType().Name.ShouldBe("CalificacionNoEncontradaException");
	}

	[Fact]
	public void ModificarObservacionCalificacion_cambia_solo_la_observacion()
	{
		var cursante = new CursanteBuilder().Build();
		var materiaID = Guid.NewGuid();
		var id = cursante.RegistrarCalificacion(materiaID, new DateTime(2026, 3, 10), Instancia.Recuperatorio, 8, "Original");

		cursante.ModificarObservacionCalificacion(id, "Modificada");

		var calificacion = cursante.Calificaciones.ShouldHaveSingleItem();
		calificacion.Id.ShouldBe(id);
		calificacion.Observacion.ShouldBe("Modificada");
		calificacion.Nota.ShouldBe(8);
		calificacion.MateriaID.ShouldBe(materiaID);
		calificacion.Fecha.ShouldBe(new DateTime(2026, 3, 10));
		calificacion.Instancia.ShouldBe(Instancia.Recuperatorio);
	}

	[Fact]
	public void ModificarObservacionCalificacion_con_mas_de_140_caracteres_lanza_ArgumentException()
	{
		var cursante = new CursanteBuilder().Build();
		var id = cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Instancia.Parcial, 8, "Original");

		Should.Throw<ArgumentException>(() => cursante.ModificarObservacionCalificacion(id, new string('a', 141)));

		cursante.Calificaciones.ShouldHaveSingleItem().Observacion.ShouldBe("Original");
	}

	[Fact]
	public void ModificarObservacionCalificacion_inexistente_lanza_CalificacionNoEncontradaException()
	{
		var cursante = new CursanteBuilder().Build();

		var ex = Should.Throw<Exception>(() => cursante.ModificarObservacionCalificacion(Guid.NewGuid(), "Nueva"));

		ex.GetType().Name.ShouldBe("CalificacionNoEncontradaException");
	}
	#endregion
}
