using Core.ServicioCursantes;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes.Exceptions;
using Core.UnitTests.Infraestructura;
using Domain.Alumnos;
using Domain.Cursantes.Calificaciones;
using Domain.Cursantes;
using Domain.Cursantes.Exceptions;
using Domain.Cursos;
using Domain.Divisiones;
using Domain.Materias;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Fakes;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioCursantes;

/// <summary>
/// <see cref="IServicioCursante"/>: inscripción, listado de cursantes, calificaciones
/// (registrar, quitar, modificar observación, listar) e inasistencia a examen, con camino feliz
/// y de error para cada caso de uso, más una prueba de <see cref="InscripcionDuplicadaException"/>.
/// Cierra H-017: <c>RegistrarCalificacionAsync_persiste_la_nota_del_cursante_en_la_materia</c> ya
/// no está <c>Skip</c>.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioCursanteTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioCursante _servicio;

	public ServicioCursanteTests()
	{
		_servicio = _host.Resolver<IServicioCursante>();
	}

	private Curso SembrarCurso()
	{
		var curso = new CursoBuilder().Build();
		_host.UnidadDeTrabajo.CursosFake.Sembrar(curso);
		return curso;
	}

	private Division SembrarDivision(Guid cursoID)
	{
		var division = new DivisionBuilder().ConCurso(cursoID).Build();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(division);
		return division;
	}

	private Alumno SembrarAlumno()
	{
		var alumno = new AlumnoBuilder().Build();
		_host.UnidadDeTrabajo.AlumnosFake.Sembrar(alumno);
		return alumno;
	}

	private Materia SembrarMateria(string descripcion = "Matemática")
	{
		var materia = new MateriaBuilder().ConDescripcion(descripcion).Build();
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(materia);
		return materia;
	}

	private Cursante SembrarCursante(Guid divisionID, Guid alumnoID, CicloLectivo cicloLectivo)
	{
		var cursante = new CursanteBuilder().ConDivision(divisionID).ConAlumno(alumnoID).ConCicloLectivo(cicloLectivo).Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);
		return cursante;
	}

	/// <summary>Inscripción cerrada: el registro histórico que deja un traslado o una baja.</summary>
	private Cursante SembrarCursanteDadoDeBaja(Guid divisionID, Guid alumnoID, CicloLectivo cicloLectivo, DateTime inicio, DateTime baja)
	{
		var cursante = new CursanteBuilder()
			.ConDivision(divisionID)
			.ConAlumno(alumnoID)
			.ConCicloLectivo(cicloLectivo)
			.ConFechaInicio(inicio)
			.DadoDeBajaEl(baja)
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);
		return cursante;
	}

	#region InscribirCursanteAsync
	[Fact]
	public async Task InscribirCursanteAsync_da_de_alta_al_cursante_y_guarda()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await _servicio.InscribirCursanteAsync(new RegistrarCursanteRequest(curso.Id, division.Id, alumno.Id, periodo, DateTime.Today));

		var cursante = _host.UnidadDeTrabajo.CursantesFake.Elementos.ShouldHaveSingleItem();
		cursante.AlumnoID.ShouldBe(alumno.Id);
		cursante.DivisionID.ShouldBe(division.Id);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task InscribirCursanteAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<InvalidOperationException>(() =>
			_servicio.InscribirCursanteAsync(new RegistrarCursanteRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), periodo, DateTime.Today)));
	}

	[Fact]
	public async Task InscribirCursanteAsync_con_el_alumno_ya_inscripto_en_el_ciclo_lanza_InscripcionDuplicadaException()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		SembrarCursante(division.Id, alumno.Id, cicloLectivo);

		await Should.ThrowAsync<InscripcionDuplicadaException>(() =>
			_servicio.InscribirCursanteAsync(new RegistrarCursanteRequest(curso.Id, division.Id, alumno.Id, cicloLectivo.Periodo, DateTime.Today)));
	}

	[Fact]
	public async Task InscribirCursanteAsync_usa_la_fecha_de_inicio_del_request_y_no_la_de_hoy()
	{
		// CUR-1: la fecha de inicio la ingresa el usuario. Antes Core la forzaba a DateTime.Today.
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var periodo = new CicloLectivoBuilder().Build().Periodo;
		var inicio = DateTime.Today.AddDays(-45);

		await _servicio.InscribirCursanteAsync(new RegistrarCursanteRequest(curso.Id, division.Id, alumno.Id, periodo, inicio));

		_host.UnidadDeTrabajo.CursantesFake.Elementos.ShouldHaveSingleItem().FechaInicio.ShouldBe(inicio);
	}

	[Fact]
	public async Task InscribirCursanteAsync_con_la_inscripcion_anterior_del_ciclo_cerrada_no_la_cuenta_como_duplicada()
	{
		// Consecuencia 2 del traslado: ExisteInscripcionActivaAsync mira sólo las activas, así que
		// un alumno con la inscripción de origen cerrada se puede volver a inscribir en el ciclo.
		var curso = SembrarCurso();
		var divisionA = SembrarDivision(curso.Id);
		var divisionB = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		SembrarCursanteDadoDeBaja(divisionA.Id, alumno.Id, cicloLectivo, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-1));

		await _servicio.InscribirCursanteAsync(
			new RegistrarCursanteRequest(curso.Id, divisionB.Id, alumno.Id, cicloLectivo.Periodo, DateTime.Today));

		_host.UnidadDeTrabajo.CursantesFake.Elementos.Count.ShouldBe(2);
	}
	#endregion

	#region ModificarCondicionDeRecursanteAsync
	[Fact]
	public async Task ModificarCondicionDeRecursanteAsync_marca_la_condicion_y_guarda()
	{
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);

		await _servicio.ModificarCondicionDeRecursanteAsync(new ModificarCondicionRecursanteRequest(cursante.Id, true));

		cursante.EsRecursante.ShouldBeTrue();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ModificarCondicionDeRecursanteAsync_con_una_inscripcion_inexistente_lanza_CursanteNoEncontradoException()
	{
		await Should.ThrowAsync<CursanteNoEncontradoException>(() =>
			_servicio.ModificarCondicionDeRecursanteAsync(new ModificarCondicionRecursanteRequest(Guid.NewGuid(), true)));
	}

	[Fact]
	public async Task ModificarCondicionDeRecursanteAsync_sobre_una_inscripcion_cerrada_propaga_la_excepcion_del_dominio()
	{
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursanteDadoDeBaja(Guid.NewGuid(), alumno.Id, cicloLectivo, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-1));

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.ModificarCondicionDeRecursanteAsync(new ModificarCondicionRecursanteRequest(cursante.Id, true)));

		ex.GetType().Name.ShouldBe("InscripcionCerradaException");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}
	#endregion

	#region RegistrarBajaDeCursanteAsync
	[Fact]
	public async Task RegistrarBajaDeCursanteAsync_cierra_la_inscripcion_y_guarda()
	{
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConCicloLectivo(cicloLectivo)
			.ConFechaInicio(DateTime.Today.AddDays(-30))
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);
		var baja = DateTime.Today.AddDays(-2);

		await _servicio.RegistrarBajaDeCursanteAsync(new RegistrarBajaCursanteRequest(cursante.Id, baja));

		cursante.FechaFin.ShouldBe(baja);
		cursante.EstaActiva().ShouldBeFalse();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarBajaDeCursanteAsync_deja_al_cursante_fuera_de_la_matricula_vigente()
	{
		// Consecuencia 3: el listado de la división es matrícula vigente, así que el dado de baja
		// desaparece de ahí y sobrevive sólo en el historial.
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(division.Id)
			.ConCicloLectivo(cicloLectivo)
			.ConFechaInicio(DateTime.Today.AddDays(-30))
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(cursante);

		await _servicio.RegistrarBajaDeCursanteAsync(new RegistrarBajaCursanteRequest(cursante.Id, DateTime.Today));

		var listado = await _servicio.ListarCursantesAsync(new BuscarListadoRequest(curso.Id, division.Id, cicloLectivo.Periodo));
		listado.ShouldBeEmpty();

		var historial = await _servicio.ConsultarHistorialDeDivisionesAsync(new ConsultarHistorialRequest(alumno.Id));
		historial.ShouldHaveSingleItem().EstaActiva.ShouldBeFalse();
	}

	[Fact]
	public async Task RegistrarBajaDeCursanteAsync_con_una_fecha_futura_propaga_la_excepcion_del_dominio()
	{
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);

		var ex = await Should.ThrowAsync<Exception>(() =>
			_servicio.RegistrarBajaDeCursanteAsync(new RegistrarBajaCursanteRequest(cursante.Id, DateTime.Today.AddDays(1))));

		ex.GetType().Name.ShouldBe("FechaBajaFuturaException");
		cursante.EstaActiva().ShouldBeTrue();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task RegistrarBajaDeCursanteAsync_con_una_inscripcion_inexistente_lanza_CursanteNoEncontradoException()
	{
		await Should.ThrowAsync<CursanteNoEncontradoException>(() =>
			_servicio.RegistrarBajaDeCursanteAsync(new RegistrarBajaCursanteRequest(Guid.NewGuid(), DateTime.Today)));
	}
	#endregion

	#region CambiarDeDivisionAsync
	[Fact]
	public async Task CambiarDeDivisionAsync_cierra_la_inscripcion_actual_y_abre_una_en_la_division_destino()
	{
		var curso = SembrarCurso();
		var divisionOrigen = SembrarDivision(curso.Id);
		var divisionDestino = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var original = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(divisionOrigen.Id)
			.ConCicloLectivo(cicloLectivo)
			.ConFechaInicio(DateTime.Today.AddDays(-30))
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(original);
		var cambio = DateTime.Today.AddDays(-1);

		await _servicio.CambiarDeDivisionAsync(
			new CambiarDivisionCursanteRequest(alumno.Id, cicloLectivo.Periodo, divisionDestino.Id, cambio));

		original.FechaFin.ShouldBe(cambio);
		var nueva = _host.UnidadDeTrabajo.CursantesFake.Elementos.Single(x => x.EstaActiva());
		nueva.DivisionID.ShouldBe(divisionDestino.Id);
		nueva.FechaInicio.ShouldBe(cambio);
		nueva.AlumnoID.ShouldBe(alumno.Id);
	}

	[Fact]
	public async Task CambiarDeDivisionAsync_guarda_una_sola_vez_porque_es_una_sola_unidad_de_trabajo()
	{
		// Cerrar la inscripción vieja y abrir la nueva son dos escrituras de la MISMA transacción:
		// con dos guardados, una falla intermedia dejaría al alumno sin inscripción activa.
		var curso = SembrarCurso();
		var divisionOrigen = SembrarDivision(curso.Id);
		var divisionDestino = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var original = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(divisionOrigen.Id)
			.ConCicloLectivo(cicloLectivo)
			.ConFechaInicio(DateTime.Today.AddDays(-30))
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(original);

		await _servicio.CambiarDeDivisionAsync(
			new CambiarDivisionCursanteRequest(alumno.Id, cicloLectivo.Periodo, divisionDestino.Id, DateTime.Today));

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task CambiarDeDivisionAsync_conserva_la_condicion_de_recursante()
	{
		var curso = SembrarCurso();
		var divisionOrigen = SembrarDivision(curso.Id);
		var divisionDestino = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var original = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(divisionOrigen.Id)
			.ConCicloLectivo(cicloLectivo)
			.ConFechaInicio(DateTime.Today.AddDays(-30))
			.ComoRecursante()
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(original);

		await _servicio.CambiarDeDivisionAsync(
			new CambiarDivisionCursanteRequest(alumno.Id, cicloLectivo.Periodo, divisionDestino.Id, DateTime.Today));

		_host.UnidadDeTrabajo.CursantesFake.Elementos.Single(x => x.EstaActiva()).EsRecursante.ShouldBeTrue();
	}

	[Fact]
	public async Task CambiarDeDivisionAsync_no_copia_las_calificaciones_a_la_inscripcion_nueva()
	{
		var curso = SembrarCurso();
		var divisionOrigen = SembrarDivision(curso.Id);
		var divisionDestino = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var materia = SembrarMateria();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var original = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(divisionOrigen.Id)
			.ConCicloLectivo(cicloLectivo)
			.ConFechaInicio(DateTime.Today.AddDays(-30))
			.Build();
		original.RegistrarCalificacion(materia.Id, DateTime.Today.AddDays(-20), Periodo.PrimerCuatrimestre, false, 8, null);
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(original);

		await _servicio.CambiarDeDivisionAsync(
			new CambiarDivisionCursanteRequest(alumno.Id, cicloLectivo.Periodo, divisionDestino.Id, DateTime.Today));

		// Las notas quedan en la inscripción cerrada: el boletín las une (DOC-1).
		original.Calificaciones.Count.ShouldBe(1);
		_host.UnidadDeTrabajo.CursantesFake.Elementos.Single(x => x.EstaActiva()).Calificaciones.ShouldBeEmpty();
	}

	[Fact]
	public async Task CambiarDeDivisionAsync_a_la_misma_division_lanza_DivisionDestinoInvalidaException()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		SembrarCursante(division.Id, alumno.Id, cicloLectivo);

		await Should.ThrowAsync<DivisionDestinoInvalidaException>(() => _servicio.CambiarDeDivisionAsync(
			new CambiarDivisionCursanteRequest(alumno.Id, cicloLectivo.Periodo, division.Id, DateTime.Today)));

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task CambiarDeDivisionAsync_sin_inscripcion_activa_lanza_NullReferenceException()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();

		await Should.ThrowAsync<NullReferenceException>(() => _servicio.CambiarDeDivisionAsync(
			new CambiarDivisionCursanteRequest(alumno.Id, cicloLectivo.Periodo, division.Id, DateTime.Today)));
	}

	[Fact]
	public async Task CambiarDeDivisionAsync_con_una_division_destino_inexistente_lanza_NullReferenceException()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		SembrarCursante(division.Id, alumno.Id, cicloLectivo);

		await Should.ThrowAsync<NullReferenceException>(() => _servicio.CambiarDeDivisionAsync(
			new CambiarDivisionCursanteRequest(alumno.Id, cicloLectivo.Periodo, Guid.NewGuid(), DateTime.Today)));
	}
	#endregion

	#region ConsultarHistorialDeDivisionesAsync
	[Fact]
	public async Task ConsultarHistorialDeDivisionesAsync_devuelve_las_inscripciones_ordenadas_por_fecha_de_inicio()
	{
		var curso = SembrarCurso();
		var divisionA = SembrarDivision(curso.Id);
		var divisionB = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		SembrarCursanteDadoDeBaja(divisionA.Id, alumno.Id, cicloLectivo, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-10));
		var vigente = new CursanteBuilder()
			.ConAlumno(alumno.Id)
			.ConDivision(divisionB.Id)
			.ConCicloLectivo(cicloLectivo)
			.ConFechaInicio(DateTime.Today.AddDays(-10))
			.Build();
		_host.UnidadDeTrabajo.CursantesFake.Sembrar(vigente);

		var historial = await _servicio.ConsultarHistorialDeDivisionesAsync(new ConsultarHistorialRequest(alumno.Id));

		historial.Count.ShouldBe(2);
		historial.First().Division.ShouldBe(divisionA.Descripcion);
		historial.First().EstaActiva.ShouldBeFalse();
		historial.First().FechaFin.ShouldBe(DateTime.Today.AddDays(-10));
		historial.Last().Division.ShouldBe(divisionB.Descripcion);
		historial.Last().EstaActiva.ShouldBeTrue();
		historial.Last().FechaFin.ShouldBeNull();
	}

	[Fact]
	public async Task ConsultarHistorialDeDivisionesAsync_describe_el_curso_y_el_ciclo_lectivo_de_cada_inscripcion()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		SembrarCursante(division.Id, alumno.Id, cicloLectivo);

		var historial = await _servicio.ConsultarHistorialDeDivisionesAsync(new ConsultarHistorialRequest(alumno.Id));

		var inscripcion = historial.ShouldHaveSingleItem();
		inscripcion.Grado.ShouldBe(curso.Grado.ToString());
		inscripcion.NivelEducativo.ShouldBe(curso.NivelEducativo.ToString());
		inscripcion.CicloLectivo.ShouldBe(cicloLectivo.Periodo);
	}

	[Fact]
	public async Task ConsultarHistorialDeDivisionesAsync_de_un_alumno_sin_inscripciones_devuelve_vacio()
	{
		var historial = await _servicio.ConsultarHistorialDeDivisionesAsync(new ConsultarHistorialRequest(Guid.NewGuid()));

		historial.ShouldBeEmpty();
	}

	[Fact]
	public async Task ConsultarHistorialDeDivisionesAsync_es_una_lectura_y_no_exige_permiso()
	{
		// Patrón vigente del repositorio de código (CORE-16): las lecturas quedan abiertas.
		using var host = new HostDeServicios(sesion: SesionDePrueba.SinPermisos());

		var historial = await host.Resolver<IServicioCursante>()
			.ConsultarHistorialDeDivisionesAsync(new ConsultarHistorialRequest(Guid.NewGuid()));

		historial.ShouldBeEmpty();
	}
	#endregion

	#region ListarCursantesAsync
	[Fact]
	public async Task ListarCursantesAsync_devuelve_los_cursantes_de_la_division()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		SembrarCursante(division.Id, alumno.Id, cicloLectivo);

		var cursantes = await _servicio.ListarCursantesAsync(new BuscarListadoRequest(curso.Id, division.Id, cicloLectivo.Periodo));

		var respuesta = cursantes.ShouldHaveSingleItem();
		respuesta.AlumnoID.ShouldBe(alumno.Id);
		respuesta.NombreCompleto.ShouldBe(alumno.DatosPersonales.NombreCompleto());
		respuesta.Documento.ShouldBe(alumno.DatosPersonales.Documento);
	}

	[Fact]
	public async Task ListarCursantesAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<InvalidOperationException>(() =>
			_servicio.ListarCursantesAsync(new BuscarListadoRequest(Guid.NewGuid(), Guid.NewGuid(), periodo)));
	}
	#endregion

	#region RegistrarCalificacionAsync (H-017)
	[Fact]
	public async Task RegistrarCalificacionAsync_con_una_inscripcion_existente_no_falla()
	{
		var alumno = SembrarAlumno();
		var materia = SembrarMateria();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);

		var calificacionID = await _servicio.RegistrarCalificacionAsync(new CrearCalificationRequest(
			alumno.Id, cicloLectivo.Periodo, materia.Id, DateTime.Today, nameof(Periodo.PrimerCuatrimestre), false, 8, "Buen desempeño"));

		calificacionID.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	public async Task RegistrarCalificacionAsync_con_una_inscripcion_inexistente_lanza_NullReferenceException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<NullReferenceException>(() => _servicio.RegistrarCalificacionAsync(
			new CrearCalificationRequest(Guid.NewGuid(), periodo, Guid.NewGuid(), DateTime.Today, nameof(Periodo.PrimerCuatrimestre), false, 8, null)));
	}

	[Fact]
	public async Task RegistrarCalificacionAsync_persiste_la_nota_del_cursante_en_la_materia()
	{
		var alumno = SembrarAlumno();
		var materia = SembrarMateria();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);

		await _servicio.RegistrarCalificacionAsync(new CrearCalificationRequest(
			alumno.Id, cicloLectivo.Periodo, materia.Id, DateTime.Today, nameof(Periodo.PrimerCuatrimestre), false, 8, "Buen desempeño"));

		var calificacion = cursante.Calificaciones.ShouldHaveSingleItem();
		calificacion.MateriaID.ShouldBe(materia.Id);
		calificacion.Nota.ShouldBe(8);
		calificacion.Rindio.ShouldBeTrue();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}
	#endregion

	#region RegistrarInasistenciaAExamenAsync
	[Fact]
	public async Task RegistrarInasistenciaAExamenAsync_registra_la_inasistencia_del_cursante()
	{
		var alumno = SembrarAlumno();
		var materia = SembrarMateria();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);

		await _servicio.RegistrarInasistenciaAExamenAsync(new RegistrarInasistenciaRequest(
			alumno.Id, cicloLectivo.Periodo, materia.Id, DateTime.Today, nameof(Periodo.PrimerCuatrimestre), false, "Ausente sin aviso"));

		var calificacion = cursante.Calificaciones.ShouldHaveSingleItem();
		calificacion.Rindio.ShouldBeFalse();
		calificacion.Nota.ShouldBeNull();
	}

	[Fact]
	public async Task RegistrarInasistenciaAExamenAsync_con_una_inscripcion_inexistente_lanza_NullReferenceException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<NullReferenceException>(() => _servicio.RegistrarInasistenciaAExamenAsync(
			new RegistrarInasistenciaRequest(Guid.NewGuid(), periodo, Guid.NewGuid(), DateTime.Today, nameof(Periodo.PrimerCuatrimestre), false, null)));
	}
	#endregion

	#region QuitarCalificacionAsync
	[Fact]
	public async Task QuitarCalificacionAsync_quita_la_calificacion_indicada_y_guarda()
	{
		var alumno = SembrarAlumno();
		var materia = SembrarMateria();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);
		var calificacionID = cursante.RegistrarCalificacion(materia.Id, DateTime.Today, Periodo.PrimerCuatrimestre, false, 8, null);

		await _servicio.QuitarCalificacionAsync(new EliminarCalificacionRequest(alumno.Id, cicloLectivo.Periodo, calificacionID));

		cursante.Calificaciones.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task QuitarCalificacionAsync_con_una_inscripcion_inexistente_lanza_NullReferenceException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.QuitarCalificacionAsync(new EliminarCalificacionRequest(Guid.NewGuid(), periodo, Guid.NewGuid())));
	}
	#endregion

	#region ModificarObservacionCalificacionAsync
	[Fact]
	public async Task ModificarObservacionCalificacionAsync_actualiza_la_observacion_de_la_calificacion()
	{
		var alumno = SembrarAlumno();
		var materia = SembrarMateria();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);
		var calificacionID = cursante.RegistrarCalificacion(materia.Id, DateTime.Today, Periodo.PrimerCuatrimestre, false, 8, "Observación original");

		await _servicio.ModificarObservacionCalificacionAsync(new ModificarObservacionCalificacionRequest(
			alumno.Id, cicloLectivo.Periodo, calificacionID, "Observación corregida"));

		var calificacion = cursante.Calificaciones.ShouldHaveSingleItem();
		calificacion.Observacion.ShouldBe("Observación corregida");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ModificarObservacionCalificacionAsync_con_una_inscripcion_inexistente_lanza_NullReferenceException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<NullReferenceException>(() => _servicio.ModificarObservacionCalificacionAsync(
			new ModificarObservacionCalificacionRequest(Guid.NewGuid(), periodo, Guid.NewGuid(), "Observación")));
	}
	#endregion

	#region ListarCalificacionesAsync
	[Fact]
	public async Task ListarCalificacionesAsync_devuelve_las_calificaciones_del_cursante()
	{
		var alumno = SembrarAlumno();
		var materia = SembrarMateria("Historia");
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);
		cursante.RegistrarCalificacion(materia.Id, DateTime.Today, Periodo.PrimerCuatrimestre, false, 9, null);

		var calificaciones = await _servicio.ListarCalificacionesAsync(new ListarCalificacionesRequest(alumno.Id, cicloLectivo.Periodo));

		var respuesta = calificaciones.ShouldHaveSingleItem();
		respuesta.MateriaID.ShouldBe(materia.Id);
		respuesta.Materia.ShouldBe("Historia");
		respuesta.Nota.ShouldBe(9);
		respuesta.Aprobado.ShouldBeTrue();
	}

	[Fact]
	public async Task ListarCalificacionesAsync_con_una_inscripcion_inexistente_lanza_NullReferenceException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<NullReferenceException>(() =>
			_servicio.ListarCalificacionesAsync(new ListarCalificacionesRequest(Guid.NewGuid(), periodo)));
	}
	#endregion
}
