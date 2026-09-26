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
/// <see cref="IServicioDivision"/>: listar, agregar y eliminar división, y asignar/quitar
/// preceptor, con camino feliz y de error para cada caso de uso, más una prueba por cada
/// excepción de <c>Core</c> creada para esta raíz (<see cref="DivisionDuplicadaException"/>,
/// <see cref="PreceptorAsignadoException"/>). Repone la intención de las pruebas de
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

	#region AgregarDivisionAsync
	[Fact]
	public async Task AgregarDivisionAsync_suma_una_division_al_curso_y_guarda()
	{
		var curso = SembrarCurso();

		await _servicio.AgregarDivisionAsync(new AgregarDivisionRequest(curso.Id));

		var division = _host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldHaveSingleItem();
		division.CursoID.ShouldBe(curso.Id);
		division.Descripcion.ShouldBe("A");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AgregarDivisionAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		await Should.ThrowAsync<InvalidOperationException>(() =>
			_servicio.AgregarDivisionAsync(new AgregarDivisionRequest(Guid.NewGuid())));
	}

	[Fact]
	public async Task AgregarDivisionAsync_con_la_descripcion_ya_registrada_lanza_DivisionDuplicadaException()
	{
		var curso = SembrarCurso();
		_host.UnidadDeTrabajo.DivisionesFake.ForzarExisteDivisionConDescripcion = true;

		await Should.ThrowAsync<DivisionDuplicadaException>(() =>
			_servicio.AgregarDivisionAsync(new AgregarDivisionRequest(curso.Id)));
	}
	#endregion

	#region EliminarDivisionAsync
	[Fact]
	public async Task EliminarDivisionAsync_elimina_la_division_indicada_y_guarda()
	{
		var curso = SembrarCurso(nivel: "Primaria");
		var division = SembrarDivision(curso.Id);
		var cicloLectivo = new CicloLectivoBuilder().Build();

		await _servicio.EliminarDivisionAsync(new EliminarDivisionRequest(curso.Id, division.Id, cicloLectivo.Periodo));

		_host.UnidadDeTrabajo.DivisionesFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarDivisionAsync_con_un_curso_inexistente_lanza_InvalidOperationException()
	{
		var cicloLectivo = new CicloLectivoBuilder().Build();

		await Should.ThrowAsync<InvalidOperationException>(() =>
			_servicio.EliminarDivisionAsync(new EliminarDivisionRequest(Guid.NewGuid(), Guid.NewGuid(), cicloLectivo.Periodo)));
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
