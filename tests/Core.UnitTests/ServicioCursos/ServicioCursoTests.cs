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
/// <see cref="IServicioCurso"/>: <c>ListarCursosAsync</c>, <c>RegistrarCurso</c> y
/// <c>EliminarCurso</c> son los únicos casos de uso que quedan en este servicio; divisiones,
/// preceptores, cursantes y calificaciones se reimplementan en servicios nuevos por agregado.
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

	private Domain.Cursos.Curso SembrarCurso(string grado = "Primero", string nivel = "Secundaria")
	{
		var curso = new CursoBuilder().ConGrado(grado).ConNivelEducativo(nivel).Build();
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
}
