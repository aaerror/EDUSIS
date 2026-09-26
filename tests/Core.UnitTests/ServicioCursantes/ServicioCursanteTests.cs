using Core.ServicioCursantes;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes.Exceptions;
using Core.UnitTests.Infraestructura;
using Domain.Alumnos;
using Domain.Cursantes;
using Domain.Cursos;
using Domain.Divisiones;
using Domain.Materias;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
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

	#region InscribirCursanteAsync
	[Fact]
	public async Task InscribirCursanteAsync_da_de_alta_al_cursante_y_guarda()
	{
		var curso = SembrarCurso();
		var division = SembrarDivision(curso.Id);
		var alumno = SembrarAlumno();
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await _servicio.InscribirCursanteAsync(new RegistrarCursanteRequest(curso.Id, division.Id, alumno.Id, periodo));

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
			_servicio.InscribirCursanteAsync(new RegistrarCursanteRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), periodo)));
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
			_servicio.InscribirCursanteAsync(new RegistrarCursanteRequest(curso.Id, division.Id, alumno.Id, cicloLectivo.Periodo)));
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
			alumno.Id, cicloLectivo.Periodo, materia.Id, DateTime.Today, "Parcial", 8, "Buen desempeño"));

		calificacionID.ShouldNotBe(Guid.Empty);
	}

	[Fact]
	public async Task RegistrarCalificacionAsync_con_una_inscripcion_inexistente_lanza_NullReferenceException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<NullReferenceException>(() => _servicio.RegistrarCalificacionAsync(
			new CrearCalificationRequest(Guid.NewGuid(), periodo, Guid.NewGuid(), DateTime.Today, "Parcial", 8, null)));
	}

	[Fact]
	public async Task RegistrarCalificacionAsync_persiste_la_nota_del_cursante_en_la_materia()
	{
		var alumno = SembrarAlumno();
		var materia = SembrarMateria();
		var cicloLectivo = new CicloLectivoBuilder().Build();
		var cursante = SembrarCursante(Guid.NewGuid(), alumno.Id, cicloLectivo);

		await _servicio.RegistrarCalificacionAsync(new CrearCalificationRequest(
			alumno.Id, cicloLectivo.Periodo, materia.Id, DateTime.Today, "Parcial", 8, "Buen desempeño"));

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
			alumno.Id, cicloLectivo.Periodo, materia.Id, DateTime.Today, "Parcial", "Ausente sin aviso"));

		var calificacion = cursante.Calificaciones.ShouldHaveSingleItem();
		calificacion.Rindio.ShouldBeFalse();
		calificacion.Nota.ShouldBeNull();
	}

	[Fact]
	public async Task RegistrarInasistenciaAExamenAsync_con_una_inscripcion_inexistente_lanza_NullReferenceException()
	{
		var periodo = new CicloLectivoBuilder().Build().Periodo;

		await Should.ThrowAsync<NullReferenceException>(() => _servicio.RegistrarInasistenciaAExamenAsync(
			new RegistrarInasistenciaRequest(Guid.NewGuid(), periodo, Guid.NewGuid(), DateTime.Today, "Parcial", null)));
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
		var calificacionID = cursante.RegistrarCalificacion(materia.Id, DateTime.Today, Domain.Cursantes.Calificaciones.Instancia.Parcial, 8, null);

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
		var calificacionID = cursante.RegistrarCalificacion(materia.Id, DateTime.Today, Domain.Cursantes.Calificaciones.Instancia.Parcial, 8, "Observación original");

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
		cursante.RegistrarCalificacion(materia.Id, DateTime.Today, Domain.Cursantes.Calificaciones.Instancia.Parcial, 9, null);

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
