using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.Exceptions;
using Core.UnitTests.Infraestructura;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioCursos;

/// <summary>
/// <see cref="IServicioCurso"/>: los 9 métodos públicos (los 2 métodos de inscripción de
/// cursantes están comentados en la interfaz, FR-013) con camino feliz y de error.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioCursoTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioCurso _servicio;

	public ServicioCursoTests()
	{
		_servicio = _host.Resolver<IServicioCurso>();
	}

	private Domain.Cursos.Curso SembrarCurso(int divisiones = 0, string grado = "Primero", string nivel = "Secundaria")
	{
		var curso = new CursoBuilder().ConGrado(grado).ConNivelEducativo(nivel).ConDivision(divisiones).Build();
		_host.UnidadDeTrabajo.CursosFake.Sembrar(curso);
		return curso;
	}

	#region Listar / Registrar / Eliminar
	[Fact]
	public async Task ListarCursosAsync_devuelve_todos_los_cursos_sembrados()
	{
		SembrarCurso(grado: "Primero");
		SembrarCurso(grado: "Segundo");

		var cursos = await _servicio.ListarCursosAsync();

		cursos.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ListarCursosAsync_sin_cursos_devuelve_una_coleccion_vacia()
	{
		var cursos = await _servicio.ListarCursosAsync();

		cursos.ShouldBeEmpty();
	}

	[Fact]
	public async Task RegistrarCurso_da_de_alta_un_curso_nuevo_y_guarda()
	{
		await _servicio.RegistrarCurso(new RegistrarCursoRequest("Primero", "Secundaria"));

		_host.UnidadDeTrabajo.CursosFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarCurso_rechaza_un_curso_con_el_mismo_grado_y_nivel_educativo()
	{
		SembrarCurso(grado: "Primero", nivel: "Secundaria");

		await Should.ThrowAsync<CursoDuplicadoException>(
			() => _servicio.RegistrarCurso(new RegistrarCursoRequest("Primero", "Secundaria")));
	}

	[Fact]
	public async Task EliminarCurso_quita_el_curso_del_repositorio()
	{
		var curso = SembrarCurso();

		await _servicio.EliminarCurso(new EliminarCursoRequest(curso.Id));

		_host.UnidadDeTrabajo.CursosFake.Elementos.ShouldBeEmpty();
	}

	[Fact]
	public async Task EliminarCurso_con_un_Id_inexistente_no_falla_y_no_borra_nada()
	{
		SembrarCurso();

		await Should.NotThrowAsync(() => _servicio.EliminarCurso(new EliminarCursoRequest(Guid.NewGuid())));
		_host.UnidadDeTrabajo.CursosFake.Elementos.ShouldHaveSingleItem();
	}
	#endregion

	#region Divisiones
	[Fact]
	public async Task BuscarDivisionesAsync_devuelve_las_divisiones_del_curso()
	{
		var curso = SembrarCurso(divisiones: 2);

		var divisiones = await _servicio.BuscarDivisionesAsync(curso.Id);

		divisiones.Count.ShouldBe(2);
	}

	[Fact]
	public async Task BuscarDivisionesAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.BuscarDivisionesAsync(Guid.NewGuid()));
	}

	[Fact]
	public async Task AgregarDivisionAlCurso_suma_una_division_al_curso_y_guarda()
	{
		var curso = SembrarCurso(divisiones: 0);

		await _servicio.AgregarDivisionAlCurso(curso.Id);

		curso.Divisiones.Count.ShouldBe(1);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AgregarDivisionAlCurso_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.AgregarDivisionAlCurso(Guid.NewGuid()));
	}

	[Fact]
	public async Task QuitarDivisiosDelCurso_elimina_la_division_indicada_y_guarda()
	{
		var curso = SembrarCurso(divisiones: 1);
		var divisionID = curso.Divisiones.First().Id;

		await _servicio.QuitarDivisiosDelCurso(new EliminarDivisionRequest(curso.Id, divisionID));

		curso.Divisiones.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task QuitarDivisiosDelCurso_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.QuitarDivisiosDelCurso(new EliminarDivisionRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion

	#region Preceptor
	[Fact]
	public async Task RegistrarPreceptorEnDivision_asigna_el_docente_como_preceptor_de_la_division()
	{
		var curso = SembrarCurso(divisiones: 1);
		var divisionID = curso.Divisiones.First().Id;
		var docenteID = Guid.NewGuid();

		await _servicio.RegistrarPreceptorEnDivision(new RegistrarPreceptorRequest(curso.Id, divisionID, docenteID));

		curso.Divisiones.First().Preceptor.ShouldBe(docenteID);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RegistrarPreceptorEnDivision_con_un_curso_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.RegistrarPreceptorEnDivision(
			new RegistrarPreceptorRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
	}

	[Fact]
	public async Task EliminarPreceptorDeDivision_deja_vacante_el_cargo_de_preceptor()
	{
		var curso = SembrarCurso(divisiones: 1);
		var divisionID = curso.Divisiones.First().Id;
		curso.AsignarPreceptor(divisionID, Guid.NewGuid());

		await _servicio.EliminarPreceptorDeDivision(new EliminarPreceptorRequest(curso.Id, divisionID));

		curso.Divisiones.First().Preceptor.ShouldBeNull();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarPreceptorDeDivision_con_un_curso_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.EliminarPreceptorDeDivision(
			new EliminarPreceptorRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion

	#region Calificación
	[Fact]
	public async Task RegistrarCalificacion_con_un_curso_existente_no_falla()
	{
		var curso = SembrarCurso(divisiones: 1);
		var divisionID = curso.Divisiones.First().Id;

		// Documenta el estado actual (H-017): el cuerpo del método sólo carga el curso y no
		// registra ninguna calificación ni guarda cambios.
		await Should.NotThrowAsync(() => _servicio.RegistrarCalificacion(
			new CrearCalificationRequest(curso.Id, divisionID, Guid.NewGuid(), Guid.NewGuid(), instancia: 1, nota: 8)));
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task RegistrarCalificacion_con_un_curso_vacio_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.RegistrarCalificacion(
			new CrearCalificationRequest(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, instancia: 1, nota: 8)));
	}

	[Fact(Skip = "H-017: ServicioCurso.RegistrarCalificacion sólo carga el curso; no crea la calificación, no modifica el agregado y no llama GuardarCambiosAsync. Ver hallazgos.md.")]
	public void RegistrarCalificacion_persiste_la_nota_del_cursante_en_la_materia()
	{
	}
	#endregion
}
