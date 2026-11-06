using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.Exceptions;
using Core.UnitTests.Infraestructura;
using Domain.Materias.DomainEvents;
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
	public async Task EliminarCurso_con_un_Id_inexistente_lanza_y_no_borra_nada()
	{
		// CAMBIO DE COMPORTAMIENTO (2026-11-06): antes era un no-op silencioso, porque el caso de uso
		// delegaba en EliminarAsync(id) sin cargar nada. Ahora necesita el curso para recorrer su
		// currícula, así que pedir el borrado de uno que no está es un error del llamador y no un
		// éxito. Es la misma ruptura que EliminarAlumnoAsync introdujo en ee75a3c.
		SembrarCurso();

		await Should.ThrowAsync<InvalidOperationException>(
			() => _servicio.EliminarCurso(new EliminarCursoRequest(Guid.NewGuid())));

		_host.UnidadDeTrabajo.CursosFake.Elementos.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task EliminarCurso_con_una_division_lanza_CursoConDivisionesException_y_no_borra_nada()
	{
		var curso = SembrarCurso();
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(new DivisionBuilder().ConCurso(curso.Id).Build());

		var ex = await Should.ThrowAsync<Exception>(
			() => _servicio.EliminarCurso(new EliminarCursoRequest(curso.Id)));

		ex.GetType().Name.ShouldBe("CursoConDivisionesException");
		_host.UnidadDeTrabajo.CursosFake.Elementos.ShouldHaveSingleItem();
	}

	[Fact]
	public async Task EliminarCurso_arrastra_las_curriculas_y_sus_materias()
	{
		var curso = SembrarCurso();
		var curricula = new CurriculaBuilder().ConCurso(curso.Id).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(new MateriaBuilder().ConCurricula(curricula.Id).Build());

		await _servicio.EliminarCurso(new EliminarCursoRequest(curso.Id));

		_host.UnidadDeTrabajo.CursosFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CurriculasFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.MateriasFake.Elementos.ShouldBeEmpty();
	}

	[Fact]
	public async Task EliminarCurso_despacha_MateriaEliminadaEvent_por_cada_materia()
	{
		// Lo que mantiene viva la cascada a cátedras: no las borra este caso de uso, las borra
		// MateriaEliminadaEventHandler al recibir el evento. Si alguien "optimizara" el doble guardado
		// a un EliminarAsync pelado, el evento no se despacharía y las cátedras quedarían huérfanas
		// trabando el DELETE de la materia contra FK_CATEDRA_MATERIA, que es Restrict.
		var curso = SembrarCurso();
		var curricula = new CurriculaBuilder().ConCurso(curso.Id).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(
			new MateriaBuilder().ConCurricula(curricula.Id).Build(),
			new MateriaBuilder().ConCurricula(curricula.Id).ConDescripcion("Historia").Build());

		await _servicio.EliminarCurso(new EliminarCursoRequest(curso.Id));

		_host.UnidadDeTrabajo.EventosPublicados
			.OfType<MateriaEliminadaEvent>()
			.Count()
			.ShouldBe(2);
	}

	[Fact]
	public async Task EliminarCurso_no_toca_la_curricula_de_otro_curso()
	{
		var curso = SembrarCurso();
		var otroCurso = SembrarCurso(grado: "Segundo");
		var ajena = new CurriculaBuilder().ConCurso(otroCurso.Id).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(ajena);
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(new MateriaBuilder().ConCurricula(ajena.Id).Build());

		await _servicio.EliminarCurso(new EliminarCursoRequest(curso.Id));

		_host.UnidadDeTrabajo.CurriculasFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.MateriasFake.Elementos.ShouldHaveSingleItem();
	}
	#endregion

	#region ConsultarImpactoEliminacionCursoAsync
	[Fact]
	public async Task ConsultarImpactoEliminacionCursoAsync_informa_curriculas_materias_y_divisiones()
	{
		var curso = SembrarCurso();
		var curricula = new CurriculaBuilder().ConCurso(curso.Id).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		_host.UnidadDeTrabajo.MateriasFake.Sembrar(new MateriaBuilder().ConCurricula(curricula.Id).Build());
		_host.UnidadDeTrabajo.DivisionesFake.Sembrar(new DivisionBuilder().ConCurso(curso.Id).Build());

		var impacto = await _servicio.ConsultarImpactoEliminacionCursoAsync(new EliminarCursoRequest(curso.Id));

		impacto.Curriculas.ShouldBe(1);
		impacto.Materias.ShouldBe(1);
		impacto.Divisiones.ShouldBe(1);
		impacto.TieneDivisiones.ShouldBeTrue();
		impacto.TieneCurricula.ShouldBeTrue();
	}

	[Fact]
	public async Task ConsultarImpactoEliminacionCursoAsync_de_un_curso_pelado_no_informa_nada()
	{
		var curso = SembrarCurso();

		var impacto = await _servicio.ConsultarImpactoEliminacionCursoAsync(new EliminarCursoRequest(curso.Id));

		impacto.TieneDivisiones.ShouldBeFalse();
		impacto.TieneCurricula.ShouldBeFalse();
	}
	#endregion
}
