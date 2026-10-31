using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.Exceptions;
using Core.UnitTests.Infraestructura;
using Domain.Cursos;
using Domain.Divisiones;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioDivisiones;

/// <summary>
/// <see cref="IServicioDivision"/>: listar, proponer la letra, agregar y eliminar división, y
/// asignar/modificar/quitar preceptor, con camino feliz y de error para cada caso de uso, más una
/// prueba por cada excepción de <c>Core</c> creada para esta raíz
/// (<see cref="DivisionDuplicadaException"/>, <see cref="PreceptorAsignadoException"/>,
/// <see cref="DivisionConInscripcionesException"/>). Repone la intención de las pruebas de
/// <c>ServicioCursoTests</c> que el Bloque A retiró al mudar divisiones y preceptores a este
/// servicio.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioDivisionTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioDivision _servicio;

	public ServicioDivisionTests()
	{
		_servicio = _host.Resolver<IServicioDivision>();
	}

	private Curso SembrarCurso(string nivel = "Secundaria")
	{
		var curso = new CursoBuilder().ConNivelEducativo(nivel).Build();
		_host.UnidadDeTrabajo.CursosFake.Sembrar(curso);
		return curso;
	}

	private Division SembrarDivision(Guid cursoID, string descripcion = "A")
	{
		var division = new DivisionBuilder().ConCurso(cursoID).ConDescripcion(descripcion).Build();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);
		return division;
	}

	#region ListarDivisionesAsync
	[Fact]
	public async Task ListarDivisionesAsync_devuelve_las_divisiones_del_curso_con_cursantes_y_preceptor()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);

		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		division.AsignarPreceptor(docente.Id);

		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = new CursanteBuilder().ConDivision(division.Id).ConCicloLectivo(cicloLectivo).Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		var divisiones = await _servicio.ListarDivisionesAsync(new ListarDivisionesRequest(curso.Id, cicloLectivo.Periodo));

		var respuesta = divisiones.ShouldHaveSingleItem();
		respuesta.DivisionID.ShouldBe(division.Id);
		respuesta.Cursantes.ShouldBe(1);
		respuesta.PreceptorID.ShouldBe(docente.Id);
		respuesta.Preceptor.ShouldBe(docente.DatosPersonales.NombreCompleto());
	}

	[Fact]
	public async Task ListarDivisionesAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		var cicloLectivo = new CicloLectivoBuilder().Build();

		await Should.ThrowAsync<InvalidOperationException>(() =>
			_servicio.ListarDivisionesAsync(new ListarDivisionesRequest(Guid.NewGuid(), cicloLectivo.Periodo)));
	}
	#endregion

	#region ProponerDescripcionDivisionAsync
	[Fact]
	public async Task ProponerDescripcionDivisionAsync_sin_divisiones_previas_propone_la_A()
	{
		var curso = SembrarCurso();

		var descripcion = await _servicio.ProponerDescripcionDivisionAsync(new ProponerDescripcionRequest(curso.Id));

		descripcion.ShouldBe("A");
	}

	[Fact]
	public async Task ProponerDescripcionDivisionAsync_propone_la_letra_siguiente_a_la_ultima_del_curso()
	{
		var curso = SembrarCurso();
		SembrarDivision(curso.Id, "A");
		SembrarDivision(curso.Id, "B");

		var descripcion = await _servicio.ProponerDescripcionDivisionAsync(new ProponerDescripcionRequest(curso.Id));

		descripcion.ShouldBe("C");
	}

	[Fact]
	public async Task ProponerDescripcionDivisionAsync_es_una_lectura_y_no_guarda_ni_crea_la_division()
	{
		var curso = SembrarCurso();

		await _servicio.ProponerDescripcionDivisionAsync(new ProponerDescripcionRequest(curso.Id));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task ProponerDescripcionDivisionAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		await Should.ThrowAsync<InvalidOperationException>(() =>
			_servicio.ProponerDescripcionDivisionAsync(new ProponerDescripcionRequest(Guid.NewGuid())));
	}
	#endregion

	#region AgregarDivisionAsync
	[Fact]
	public async Task AgregarDivisionAsync_suma_una_division_al_curso_y_guarda()
	{
		var curso = SembrarCurso();

		await _servicio.AgregarDivisionAsync(new AgregarDivisionRequest(curso.Id, "A"));

		var division = _host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldHaveSingleItem();
		division.CursoID.ShouldBe(curso.Id);
		division.Descripcion.ShouldBe("A");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AgregarDivisionAsync_registra_la_letra_que_eligio_el_usuario_y_no_la_propuesta()
	{
		// DIV-1: la letra se propone y el usuario la puede cambiar. Con la "A" ya registrada, la
		// propuesta sería "B"; el request pide la "D" y es la que tiene que quedar.
		var curso = SembrarCurso();
		SembrarDivision(curso.Id, "A");

		await _servicio.AgregarDivisionAsync(new AgregarDivisionRequest(curso.Id, "D"));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos
			.Select(x => x.Descripcion)
			.ShouldBe(new[] { "A", "D" });
	}

	[Fact]
	public async Task AgregarDivisionAsync_con_una_descripcion_que_no_es_una_letra_lanza_ArgumentException()
	{
		var curso = SembrarCurso();

		await Should.ThrowAsync<ArgumentException>(() =>
			_servicio.AgregarDivisionAsync(new AgregarDivisionRequest(curso.Id, "AB")));
	}

	[Fact]
	public async Task AgregarDivisionAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		await Should.ThrowAsync<InvalidOperationException>(() =>
			_servicio.AgregarDivisionAsync(new AgregarDivisionRequest(Guid.NewGuid(), "A")));
	}

	[Fact]
	public async Task AgregarDivisionAsync_con_la_descripcion_ya_registrada_lanza_DivisionDuplicadaException()
	{
		var curso = SembrarCurso();
		SembrarDivision(curso.Id, "A");

		await Should.ThrowAsync<DivisionDuplicadaException>(() =>
			_servicio.AgregarDivisionAsync(new AgregarDivisionRequest(curso.Id, "A")));
	}

	[Fact]
	public async Task AgregarDivisionAsync_con_una_carrera_entre_dos_altas_lanza_DivisionDuplicadaException()
	{
		// La bandera del fake simula que otra alta concurrente registró la misma letra entre la
		// propuesta y el alta: la guarda de Core es lo único que lo detecta.
		var curso = SembrarCurso();
		_host.UnidadDeTrabajo.DivisionesFake.ForzarExisteDivisionConDescripcion = true;

		await Should.ThrowAsync<DivisionDuplicadaException>(() =>
			_servicio.AgregarDivisionAsync(new AgregarDivisionRequest(curso.Id, "A")));
	}
	#endregion

	#region EliminarDivisionAsync
	[Theory]
	[InlineData("Primaria")]
	[InlineData("Secundaria")]
	public async Task EliminarDivisionAsync_elimina_la_division_vacia_de_cualquier_nivel_y_guarda(string nivel)
	{
		// DIV-2: con la división vacía se elimina siempre. Secundaria entra acá porque antes el
		// dominio la rechazaba incluso sin cursantes.
		var curso = SembrarCurso(nivel);
		var division = SembrarDivision(curso.Id);
		var cicloLectivo = new CicloLectivoBuilder().Build();

		await _servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id, cicloLectivo.Periodo));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarDivisionAsync_con_una_inscripcion_activa_lanza_DivisionConInscripcionesException()
	{
		var curso = SembrarCurso(nivel: "Primaria");
		var division = SembrarDivision(curso.Id);
		var cicloLectivo = new CicloLectivoBuilder().Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(
			new CursanteBuilder().ConDivision(division.Id).ConCicloLectivo(cicloLectivo).Build());

		var ex = await Should.ThrowAsync<DivisionConInscripcionesException>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id, cicloLectivo.Periodo)));

		ex.Message.ShouldContain("dar de baja o trasladar");
		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task EliminarDivisionAsync_con_una_inscripcion_cerrada_tambien_lanza_DivisionConInscripcionesException()
	{
		// El caso que motiva la guarda: la matrícula vigente de la división es cero —la inscripción
		// está cerrada y de un ciclo anterior—, así que el dominio la dejaría eliminar, pero
		// FK_CURSANTE_DIVISION es Restrict y el DELETE fallaría en SQL Server.
		var curso = SembrarCurso(nivel: "Primaria");
		var division = SembrarDivision(curso.Id);
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cicloAnterior = new CicloLectivoBuilder().ConAño(DateTime.Today.Year - 1).Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(new CursanteBuilder()
			.ConDivision(division.Id)
			.ConCicloLectivo(cicloAnterior)
			.ConFechaInicio(DateTime.Today.AddYears(-1))
			.DadoDeBajaEl(DateTime.Today.AddMonths(-6))
			.Build());

		await Should.ThrowAsync<DivisionConInscripcionesException>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id, cicloLectivo.Periodo)));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task EliminarDivisionAsync_de_secundaria_con_matricula_vigente_lanza_DivisionNoEliminableException()
	{
		// La regla de dominio corre ANTES de la guarda de inscripciones, así que con alumnos
		// cursando el usuario lee el mensaje del nivel educativo y no "dar de baja o trasladar".
		// Si acá apareciera DivisionConInscripcionesException, las dos guardas están invertidas.
		var curso = SembrarCurso(nivel: "Secundaria");
		var division = SembrarDivision(curso.Id);
		var cicloLectivo = new CicloLectivoBuilder().Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(
			new CursanteBuilder().ConDivision(division.Id).ConCicloLectivo(cicloLectivo).Build());

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id, cicloLectivo.Periodo)));

		ex.GetType().Name.ShouldBe("DivisionNoEliminableException");
		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task EliminarDivisionAsync_de_primaria_con_matricula_suficiente_lanza_DivisionConMatriculaSuficienteException()
	{
		var curso = SembrarCurso(nivel: "Primaria");
		var division = SembrarDivision(curso.Id);
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursantes = Enumerable.Range(0, 15)
			.Select(_ => new CursanteBuilder().ConDivision(division.Id).ConCicloLectivo(cicloLectivo).Build())
			.ToArray();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursantes);

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id, cicloLectivo.Periodo)));

		ex.GetType().Name.ShouldBe("DivisionConMatriculaSuficienteException");
		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task EliminarDivisionAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		var cicloLectivo = new CicloLectivoBuilder().Build();

		await Should.ThrowAsync<InvalidOperationException>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(Guid.NewGuid(), Guid.NewGuid(), cicloLectivo.Periodo)));
	}

	[Fact]
	public async Task EliminarDivisionAsync_con_una_division_inexistente_lanza_NullReferenceException()
	{
		var curso = SembrarCurso();
		var cicloLectivo = new CicloLectivoBuilder().Build();

		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, Guid.NewGuid(), cicloLectivo.Periodo)));
	}
	#endregion

	#region AsignarPreceptorAsync
	[Fact]
	public async Task AsignarPreceptorAsync_asigna_el_docente_como_preceptor_de_la_division()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		await _servicio.AsignarPreceptorAsync(new RegistrarPreceptorRequest(curso.Id, division.Id, docente.Id));

		division.Preceptor.ShouldBe(docente.Id);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AsignarPreceptorAsync_con_una_division_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.AsignarPreceptorAsync(new RegistrarPreceptorRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task AsignarPreceptorAsync_con_un_docente_ya_preceptor_de_otra_division_lanza_PreceptorAsignadoException()
	{
		var curso = SembrarCurso();
		var divisionA = SembrarDivision(curso.Id, "A");
		var divisionB = SembrarDivision(curso.Id, "B");
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		divisionA.AsignarPreceptor(docente.Id);

		await Should.ThrowAsync<PreceptorAsignadoException>(() =>
			_servicio.AsignarPreceptorAsync(new RegistrarPreceptorRequest(curso.Id, divisionB.Id, docente.Id)));
	}
	#endregion

	#region ModificarPreceptorAsync
	[Fact]
	public async Task ModificarPreceptorAsync_reemplaza_al_preceptor_de_la_division_y_guarda()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var anterior = new DocenteBuilder().Build();
		var nuevo = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(anterior);
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(nuevo);
		division.AsignarPreceptor(anterior.Id);

		await _servicio.ModificarPreceptorAsync(new ModificarPreceptorRequest(curso.Id, division.Id, nuevo.Id));

		division.Preceptor.ShouldBe(nuevo.Id);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ModificarPreceptorAsync_con_una_division_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.ModificarPreceptorAsync(new ModificarPreceptorRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task ModificarPreceptorAsync_con_un_docente_ya_preceptor_de_otra_division_lanza_PreceptorAsignadoException()
	{
		var curso = SembrarCurso();
		var divisionA = SembrarDivision(curso.Id, "A");
		var divisionB = SembrarDivision(curso.Id, "B");
		var preceptorDeA = new DocenteBuilder().Build();
		var preceptorDeB = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(preceptorDeA);
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(preceptorDeB);
		divisionA.AsignarPreceptor(preceptorDeA.Id);
		divisionB.AsignarPreceptor(preceptorDeB.Id);

		await Should.ThrowAsync<PreceptorAsignadoException>(() =>
			_servicio.ModificarPreceptorAsync(new ModificarPreceptorRequest(curso.Id, divisionB.Id, preceptorDeA.Id)));

		divisionB.Preceptor.ShouldBe(preceptorDeB.Id);
	}

	[Fact]
	public async Task ModificarPreceptorAsync_sobre_un_cargo_vacante_lanza_CargoPreceptorDisponibleException()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.ModificarPreceptorAsync(new ModificarPreceptorRequest(curso.Id, division.Id, docente.Id)));

		ex.GetType().Name.ShouldBe("CargoPreceptorDisponibleException");
	}

	[Fact]
	public async Task ModificarPreceptorAsync_con_el_preceptor_que_ya_tiene_lo_rechaza_por_la_guarda_de_Core()
	{
		// La guarda ExistePreceptorAsignadoAsync mira todas las divisiones, la propia incluida, así
		// que corta antes que Division.CambiarPreceptor y el que llega es PreceptorAsignadoException
		// y no PreceptorSinCambiosException. La regla del dominio se verifica en DivisionTests.
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		division.AsignarPreceptor(docente.Id);

		await Should.ThrowAsync<PreceptorAsignadoException>(() =>
			_servicio.ModificarPreceptorAsync(new ModificarPreceptorRequest(curso.Id, division.Id, docente.Id)));

		division.Preceptor.ShouldBe(docente.Id);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}
	#endregion

	#region QuitarPreceptorAsync
	[Fact]
	public async Task QuitarPreceptorAsync_deja_vacante_el_cargo_de_preceptor()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		division.AsignarPreceptor(docente.Id);

		await _servicio.QuitarPreceptorAsync(new EliminarPreceptorRequest(curso.Id, division.Id));

		division.EstaCargoPreceptorVacante().ShouldBeTrue();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task QuitarPreceptorAsync_con_una_division_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.QuitarPreceptorAsync(new EliminarPreceptorRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion
}
