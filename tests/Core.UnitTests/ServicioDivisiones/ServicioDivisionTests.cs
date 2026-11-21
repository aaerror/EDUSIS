using Core.ServicioCursos.Exceptions;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioDivisiones.Exceptions;
using Core.UnitTests.Infraestructura;
using Domain.Asistencias;
using Domain.Catedras.SituacionesRevista;
using Domain.Cursantes;
using Domain.Cursantes.Calificaciones;
using Domain.Cursos;
using Domain.Divisiones;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioDivisiones;

/// <summary>
/// <see cref="IServicioDivision"/>: listar, proponer la letra, agregar y eliminar división, consultar
/// el impacto del borrado, y asignar/modificar/quitar preceptor, con camino feliz y de error para
/// cada caso de uso, más una prueba por cada excepción de <c>Core</c> creada para esta raíz
/// (<see cref="DivisionDuplicadaException"/>, <see cref="PreceptorAsignadoException"/>). Repone la
/// intención de las pruebas de <c>ServicioCursoTests</c> que se retiraron al mudar divisiones
/// y preceptores a este servicio.
/// </summary>
/// <remarks>
/// <c>DivisionConInscripcionesException</c> se borró con la
/// guarda que la lanzaba: eliminar una división pasó de rechazar ante una inscripción histórica a
/// cascadear la matrícula entera.
/// </remarks>
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
	public async Task ListarDivisionesAsync_con_un_curso_inexistente_lanza_CursoNoEncontrado()
	{
		var cicloLectivo = new CicloLectivoBuilder().Build();

		await Should.ThrowAsync<CursoNoEncontradoException>(() =>
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
	public async Task ProponerDescripcionDivisionAsync_con_un_curso_inexistente_lanza_CursoNoEncontrado()
	{
		await Should.ThrowAsync<CursoNoEncontradoException>(() =>
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
	public async Task AgregarDivisionAsync_con_una_descripcion_que_no_es_una_letra_lanza_DescripcionDivisionInvalidaException()
	{
		var curso = SembrarCurso();

		await Should.ThrowAsync<Domain.Divisiones.Exceptions.DescripcionDivisionInvalidaException>(() =>
			_servicio.AgregarDivisionAsync(new AgregarDivisionRequest(curso.Id, "AB")));
	}

	[Fact]
	public async Task AgregarDivisionAsync_con_un_curso_inexistente_lanza_CursoNoEncontrado()
	{
		await Should.ThrowAsync<CursoNoEncontradoException>(() =>
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
	// Las cinco pruebas que había acá afirmaban las DOS guardas que este caso de uso tenía antes:
	// la regla de negocio por nivel educativo (DivisionNoEliminableException,
	// DivisionConMatriculaSuficienteException) y la guarda de integridad por inscripciones
	// (DivisionConInscripcionesException). Las tres excepciones se borraron junto con las guardas: la
	// división pasa a eliminarse SIEMPRE, cascadeando lo que cuelga de ella.
	[Theory]
	[InlineData("Primaria")]
	[InlineData("Secundaria")]
	public async Task EliminarDivisionAsync_elimina_la_division_vacia_de_cualquier_nivel(string nivel)
	{
		var curso = SembrarCurso(nivel);
		var division = SembrarDivision(curso.Id);

		await _servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldContain("CommitTransactionAsync");
	}

	[Theory]
	[InlineData("Primaria")]
	[InlineData("Secundaria")]
	public async Task EliminarDivisionAsync_con_matricula_vigente_ya_no_rechaza_y_la_arrastra(string nivel)
	{
		// El vuelco de política: con alumnos cursando, los dos niveles rechazaban el borrado. Ahora
		// los dos lo hacen y se llevan la matrícula. Es la prueba que falla si alguien repone la
		// regla de nivel educativo.
		var curso = SembrarCurso(nivel);
		var division = SembrarDivision(curso.Id);
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursantes = Enumerable.Range(0, 15)
			.Select(_ => new CursanteBuilder().ConDivision(division.Id).ConCicloLectivo(cicloLectivo).Build())
			.ToArray();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursantes);

		await _servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CursantesFake.Elementos.ShouldBeEmpty();
	}

	[Fact]
	public async Task EliminarDivisionAsync_arrastra_catedras_planillas_cursantes_y_calificaciones()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);

		var cursante = new CursanteBuilder().ConDivision(division.Id).Build();
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Periodo.PrimerCuatrimestre, false, 8, null);
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(
			new PlanillaAsistenciaBuilder().ConDivision(division.Id).ConCursantes(cursante.Id).Build());

		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(
			new CatedraBuilder().ConDivision(division.Id).Build());

		await _servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CursantesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CatedrasFake.Elementos.ShouldBeEmpty();
	}

	[Fact]
	public async Task EliminarDivisionAsync_no_toca_lo_que_es_de_otra_division()
	{
		// El fake es LINQ to Objects y filtra por DivisionID igual que el repositorio real: si alguna
		// de las tres consultas del caso de uso dejara de filtrar, esta prueba lo ve.
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var otraDivision = SembrarDivision(curso.Id, "B");

		_host.UnidadDeTrabajo.CursantesFake.Sembrar(
			new CursanteBuilder().ConDivision(otraDivision.Id).Build());
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(
			new PlanillaAsistenciaBuilder().ConDivision(otraDivision.Id).Build());
		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(
			new CatedraBuilder().ConDivision(otraDivision.Id).Build());

		await _servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CursantesFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CatedrasFake.Elementos.ShouldHaveSingleItem();
	}

	// La guarda de permiso y la secuencia de transacción de este caso de uso están en
	// AutorizacionDeCasosDeUsoTests, que es donde viven las 72 guardas: EliminarDivisionAsync_sin_
	// permiso_no_toca_la_transaccion.
	[Fact]
	public async Task EliminarDivisionAsync_con_un_curso_inexistente_lanza_CursoNoEncontrado()
	{
		await Should.ThrowAsync<CursoNoEncontradoException>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task EliminarDivisionAsync_con_una_division_inexistente_lanza_DivisionNoEncontrada()
	{
		var curso = SembrarCurso();

		await Should.ThrowAsync<DivisionNoEncontradaException>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, Guid.NewGuid())));
	}
	#endregion

	#region ConsultarImpactoEliminacionDivisionAsync
	[Fact]
	public async Task ConsultarImpactoEliminacionDivisionAsync_informa_exactamente_lo_que_el_borrado_destruye()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);

		var cursante = new CursanteBuilder().ConDivision(division.Id).Build();
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Periodo.PrimerCuatrimestre, false, 8, null);
		cursante.RegistrarCalificacion(Guid.NewGuid(), DateTime.Today, Periodo.PrimerCuatrimestre, false, 9, null);
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(
			new PlanillaAsistenciaBuilder().ConDivision(division.Id).ConCursantes(cursante.Id).Build());
		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(
			new CatedraBuilder().ConDivision(division.Id).Build());

		var impacto = await _servicio.ConsultarImpactoEliminacionDivisionAsync(
			new EliminarDivisionRequest(curso.Id, division.Id));

		impacto.Inscripciones.ShouldBe(1);
		impacto.Calificaciones.ShouldBe(2);
		impacto.Planillas.ShouldBe(1);
		impacto.RegistrosAsistencia.ShouldBe(1);
		impacto.Catedras.ShouldBe(1);
		impacto.TieneHistorial.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarImpactoEliminacionDivisionAsync_de_una_division_vacia_no_tiene_historial()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);

		var impacto = await _servicio.ConsultarImpactoEliminacionDivisionAsync(
			new EliminarDivisionRequest(curso.Id, division.Id));

		impacto.TieneHistorial.ShouldBeFalse();
		impacto.CiclosLectivos.ShouldBeEmpty();
	}

	// Que esta lectura exija permiso —la asimetría deliberada frente a las demás lecturas, que quedan abiertas— está afirmado en
	// AutorizacionDeCasosDeUsoTests, junto a la de curso y la de alumno.
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
	public async Task AsignarPreceptorAsync_con_una_division_inexistente_lanza_DivisionNoEncontrada()
	{
		await Should.ThrowAsync<DivisionNoEncontradaException>(() =>
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
	public async Task ModificarPreceptorAsync_con_una_division_inexistente_lanza_DivisionNoEncontrada()
	{
		await Should.ThrowAsync<DivisionNoEncontradaException>(() =>
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
	public async Task QuitarPreceptorAsync_con_una_division_inexistente_lanza_DivisionNoEncontrada()
	{
		await Should.ThrowAsync<DivisionNoEncontradaException>(() =>
			_servicio.QuitarPreceptorAsync(new EliminarPreceptorRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion

	#region ConsultarResumenDivisionAsync
	// Mitad del ciclo 2026: hay días anteriores dentro del año para sembrar planillas, y el ciclo
	// pedido es el año en curso. El reloj se fija dentro de cada prueba (ver Reloj).
	private static readonly DateTime Hoy = new(2026, 6, 15);
	private static readonly DateTime InicioDeClases = new(2026, 3, 1);
	private const string Ciclo = "2026";

	private Task<ResumenDivisionResponse> ConsultarResumen(Guid divisionID, string ciclo = Ciclo) =>
		_servicio.ConsultarResumenDivisionAsync(new ConsultarResumenDivisionRequest(divisionID, ciclo));

	private Cursante SembrarCursante(Guid divisionID, string ciclo = Ciclo, bool recursante = false, DateTime? baja = null, Guid? alumnoID = null)
	{
		var builder = new CursanteBuilder()
			.ConDivision(divisionID)
			.ConAlumno(alumnoID ?? Guid.NewGuid())
			.ConCicloLectivo(new CicloLectivoBuilder().ConPeriodo(ciclo).Build())
			.ConFechaInicio(InicioDeClases)
			.ComoRecursante(recursante);

		if (baja.HasValue)
		{
			builder.DadoDeBajaEl(baja.Value);
		}

		var cursante = builder.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);
		return cursante;
	}

	/// <summary>
	/// Siembra la planilla con todos presentes. Las marcas van en <paramref name="marcar"/> y se
	/// aplican antes de cerrarla, porque una planilla cerrada ya no admite cambios.
	/// </summary>
	private void SembrarPlanilla(Guid divisionID, DateTime fecha, bool cerrada, Cursante[] cursantes, Action<PlanillaAsistencia>? marcar = null)
	{
		var planilla = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(fecha)
			.ConCursantes(cursantes.Select(x => x.Id).ToArray())
			.Build();

		marcar?.Invoke(planilla);

		if (cerrada)
		{
			planilla.Cerrar();
		}

		_host.UnidadDeTrabajo.PlanillasAsistenciaFake.Sembrar(planilla);
	}

	[Fact]
	public async Task ConsultarResumenDivisionAsync_informa_la_matricula_del_ciclo_con_recursantes_y_bajas()
	{
		using var _ = RelojFijo.En(Hoy);
		var division = SembrarDivision(SembrarCurso().Id);
		SembrarCursante(division.Id);
		SembrarCursante(division.Id, recursante: true);
		SembrarCursante(division.Id, baja: new DateTime(2026, 5, 1));
		SembrarCursante(division.Id, ciclo: "2025");

		var resumen = await ConsultarResumen(division.Id);

		resumen.Inscriptos.ShouldBe(2);
		resumen.Recursantes.ShouldBe(1);
		resumen.Bajas.ShouldBe(1);
		resumen.Cupo.ShouldBe(Division.MAX_ALUMNOS);
	}

	/// <remarks>
	/// Criterio A: un alumno con 8 y 6 (promedio 7) y otro con 4 dan 5,5, no 6. El alumno dado de baja
	/// tiene un 10 que no entra: el resumen habla de la matrícula vigente.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenDivisionAsync_promedia_a_los_alumnos_y_cuenta_los_que_tienen_materias_desaprobadas()
	{
		using var _ = RelojFijo.En(Hoy);
		var division = SembrarDivision(SembrarCurso().Id);
		var (matematica, lengua) = (Guid.NewGuid(), Guid.NewGuid());

		var bueno = SembrarCursante(division.Id);
		bueno.RegistrarCalificacion(matematica, Hoy, Periodo.Final, false, 8, null);
		bueno.RegistrarCalificacion(lengua, Hoy, Periodo.Final, false, 6, null);

		var flojo = SembrarCursante(division.Id);
		flojo.RegistrarCalificacion(matematica, Hoy, Periodo.Final, false, 4, null);
		flojo.RegistrarInasistenciaAExamen(lengua, Hoy, Periodo.PrimerCuatrimestre, false, null);

		var dadoDeBaja = SembrarCursante(division.Id, baja: new DateTime(2026, 5, 1));
		dadoDeBaja.RegistrarCalificacion(matematica, new DateTime(2026, 4, 1), Periodo.PrimerCuatrimestre, false, 10, null);

		var resumen = await ConsultarResumen(division.Id);

		resumen.Promedio.ShouldBe(5.5m);
		resumen.AlumnosConMateriasDesaprobadas.ShouldBe(1);
		resumen.AusenciasAExamen.ShouldBe(1);
	}

	[Fact]
	public async Task ConsultarResumenDivisionAsync_informa_la_asistencia_del_ciclo_y_los_alumnos_con_mas_faltas()
	{
		using var _ = RelojFijo.En(Hoy);
		var division = SembrarDivision(SembrarCurso().Id);
		var alumno = new AlumnoBuilder().Build();
		_host.UnidadDeTrabajo.AlumnosFake.Sembrar(alumno);
		var faltador = SembrarCursante(division.Id, alumnoID: alumno.Id);
		var presente = SembrarCursante(division.Id);

		var ambos = new[] { faltador, presente };

		// Tres planillas del ciclo: 6 marcas, una falta (83,3 %) y una tardanza, que cuenta como
		// asistida. La de 2025 cae fuera del período y no entra a ninguna cifra.
		SembrarPlanilla(division.Id, new DateTime(2026, 6, 10), cerrada: true, ambos, p => p.MarcarAusencia(faltador.Id));
		SembrarPlanilla(division.Id, new DateTime(2026, 6, 11), cerrada: false, ambos, p => p.MarcarTardanza(faltador.Id, TimeSpan.FromMinutes(10)));
		SembrarPlanilla(division.Id, Hoy, cerrada: false, ambos);
		SembrarPlanilla(division.Id, new DateTime(2025, 11, 1), cerrada: false, ambos, p => p.MarcarAusencia(faltador.Id));

		var resumen = await ConsultarResumen(division.Id);

		resumen.Planillas.ShouldBe(3);
		resumen.PlanillasSinCerrar.ShouldBe(1);
		resumen.PlanillaDeHoy.ShouldBe(EstadoPlanillaDelDia.Abierta);
		resumen.Faltas.ShouldBe(1);
		resumen.Tardanzas.ShouldBe(1);
		resumen.PorcentajeAsistencia.ShouldBe(83.3m);

		var conMasFaltas = resumen.AlumnosConMasFaltas.ShouldHaveSingleItem();
		conMasFaltas.AlumnoID.ShouldBe(alumno.Id);
		conMasFaltas.NombreCompleto.ShouldBe(alumno.DatosPersonales.NombreCompleto());
		conMasFaltas.Faltas.ShouldBe(1);
	}

	[Fact]
	public async Task ConsultarResumenDivisionAsync_sin_planillas_no_informa_porcentaje_de_asistencia()
	{
		using var _ = RelojFijo.En(Hoy);
		var division = SembrarDivision(SembrarCurso().Id);

		var resumen = await ConsultarResumen(division.Id);

		resumen.PorcentajeAsistencia.ShouldBeNull();
		resumen.PlanillaDeHoy.ShouldBe(EstadoPlanillaDelDia.SinAbrir);
		resumen.Promedio.ShouldBeNull();
	}

	[Fact]
	public async Task ConsultarResumenDivisionAsync_informa_catedras_sin_docente_y_materias_de_la_curricula_sin_catedra()
	{
		using var _ = RelojFijo.En(Hoy);
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);

		var curricula = new CurriculaBuilder().ConCurso(curso.Id).ConFechaInicio(new DateTime(2020, 1, 1)).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		var matematica = new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion("Matemática").Build();
		var lengua = new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion("Lengua").Build();
		var historia = new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion("Historia").Build();
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(matematica);
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(lengua);
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(historia);

		var conDocente = new CatedraBuilder().ConMateria(matematica.Id).ConDivision(division.Id).ConCargaHoraria(4).Build();
		conDocente.PonerEnFunciones(conDocente.Designar(Guid.NewGuid(), Cargo.Titular, InicioDeClases, null));
		var sinDocente = new CatedraBuilder().ConMateria(lengua.Id).ConDivision(division.Id).ConCargaHoraria(3).Build();
		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(conDocente);
		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(sinDocente);

		var resumen = await ConsultarResumen(division.Id);

		resumen.CatedrasSinDocente.ShouldBe(1);
		resumen.CatedrasConSuplente.ShouldBe(0);
		resumen.HorasSinAsignar.ShouldBe(7);
		resumen.MateriasSinCatedra.ShouldBe(new[] { "Historia" });
	}

	[Fact]
	public async Task ConsultarResumenDivisionAsync_cuenta_la_catedra_atendida_por_un_suplente()
	{
		using var _ = RelojFijo.En(Hoy);
		var division = SembrarDivision(SembrarCurso().Id);

		var catedra = new CatedraBuilder().ConDivision(division.Id).Build();
		var titular = catedra.Designar(Guid.NewGuid(), Cargo.Titular, InicioDeClases, null);
		catedra.PonerEnFunciones(catedra.Designar(Guid.NewGuid(), Cargo.Suplente, new DateTime(2026, 6, 1), new DateTime(2026, 7, 31), reemplazaA: titular));
		_host.UnidadDeTrabajo.CatedrasFake.Sembrar(catedra);

		var resumen = await ConsultarResumen(division.Id);

		resumen.CatedrasSinDocente.ShouldBe(0);
		resumen.CatedrasConSuplente.ShouldBe(1);
	}

	[Fact]
	public async Task ConsultarResumenDivisionAsync_avisa_que_el_preceptor_esta_de_licencia()
	{
		using var _ = RelojFijo.En(Hoy);
		var division = SembrarDivision(SembrarCurso().Id);
		var preceptor = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(preceptor);
		division.AsignarPreceptor(preceptor.Id);
		_host.UnidadDeTrabajo.LicenciasFake.Sembrar(new LicenciaBuilder().ConDocente(preceptor.Id).ConFechaInicio(new DateTime(2026, 6, 1)).Aprobada().Build());

		var resumen = await ConsultarResumen(division.Id);

		resumen.PreceptorDeLicencia.ShouldBe(true);
	}

	/// <remarks>
	/// Lo que describe el presente no tiene sentido para un año cerrado: la planilla "de hoy" de 2025
	/// no existe, y cátedras y licencias no tienen ciclo. Matrícula y asistencia sí se informan.
	/// </remarks>
	[Fact]
	public async Task ConsultarResumenDivisionAsync_de_un_ciclo_pasado_no_informa_el_estado_de_hoy()
	{
		using var _ = RelojFijo.En(Hoy);
		var division = SembrarDivision(SembrarCurso().Id);
		var cursante = SembrarCursante(division.Id, ciclo: "2025");
		SembrarPlanilla(division.Id, new DateTime(2025, 11, 1), cerrada: false, new[] { cursante });

		var resumen = await ConsultarResumen(division.Id, "2025");

		resumen.EsCicloEnCurso.ShouldBeFalse();
		resumen.Inscriptos.ShouldBe(1);
		resumen.Planillas.ShouldBe(1);
		resumen.PlanillasSinCerrar.ShouldBe(1);
		resumen.PlanillaDeHoy.ShouldBeNull();
		resumen.PreceptorDeLicencia.ShouldBeNull();
		resumen.CatedrasSinDocente.ShouldBeNull();
		resumen.MateriasSinCatedra.ShouldBeNull();
	}

	[Fact]
	public async Task ConsultarResumenDivisionAsync_es_una_lectura_y_no_guarda()
	{
		using var _ = RelojFijo.En(Hoy);
		var division = SembrarDivision(SembrarCurso().Id);

		await ConsultarResumen(division.Id);

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task ConsultarResumenDivisionAsync_con_una_division_inexistente_lanza_DivisionNoEncontrada()
	{
		await Should.ThrowAsync<DivisionNoEncontradaException>(() => ConsultarResumen(Guid.NewGuid()));
	}
	#endregion
}
