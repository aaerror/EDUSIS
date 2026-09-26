using Core.ServicioCurriculas;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.UnitTests.Infraestructura;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioCurriculas;

/// <summary>
/// <see cref="IServicioCurricula"/>: <c>ListarCurriculasSegunCursoAsync</c>,
/// <c>RegistrarCurriculaAsync</c> y <c>DesafectarCurriculaAsync</c> son los únicos casos de uso
/// que quedan en este servicio; materias y situación de revista se reimplementan en servicios
/// nuevos por agregado (<c>ServicioMateria</c>, <c>ServicioCatedra</c>).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioCurriculaTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioCurricula _servicio;

	public ServicioCurriculaTests()
	{
		_servicio = _host.Resolver<IServicioCurricula>();
	}

	private Domain.Curriculas.Curricula SembrarCurricula(Guid cursoID)
	{
		var curricula = new CurriculaBuilder().ConCurso(cursoID).Build();
		_host.UnidadDeTrabajo.CurriculasFake.Sembrar(curricula);
		return curricula;
	}

	#region Currículas
	[Fact]
	public async Task ListarCurriculasSegunCursoAsync_devuelve_las_curriculas_del_curso()
	{
		var cursoID = Guid.NewGuid();
		SembrarCurricula(cursoID);
		SembrarCurricula(Guid.NewGuid());

		var curriculas = await _servicio.ListarCurriculasSegunCursoAsync(new CursoRequest(cursoID));

		curriculas.ShouldHaveSingleItem();
		curriculas.First().CursoID.ShouldBe(cursoID);
	}

	[Fact]
	public async Task ListarCurriculasSegunCursoAsync_sin_curriculas_devuelve_coleccion_vacia()
	{
		var curriculas = await _servicio.ListarCurriculasSegunCursoAsync(new CursoRequest(Guid.NewGuid()));

		curriculas.ShouldBeEmpty();
	}

	[Fact]
	public async Task RegistrarCurriculaAsync_da_de_alta_la_curricula_dentro_de_una_transaccion()
	{
		var cursoID = Guid.NewGuid();

		await _servicio.RegistrarCurriculaAsync(new RegistrarCurriculaRequest(cursoID, DateTime.Today, null));

		_host.UnidadDeTrabajo.CurriculasFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldContain("BeginTransactionAsync");
		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldContain("CommitTransactionAsync");
	}

	[Fact]
	public async Task RegistrarCurriculaAsync_ante_un_error_hace_rollback_y_propaga()
	{
		// FechaFin anterior a FechaInicio: el constructor de RangoFechas revienta dentro del try.
		await Should.ThrowAsync<Exception>(() => _servicio.RegistrarCurriculaAsync(
			new RegistrarCurriculaRequest(Guid.NewGuid(), DateTime.Today, DateTime.Today.AddDays(-10))));

		_host.UnidadDeTrabajo.LlamadasDeTransaccion.ShouldContain("RollbackTransactionAsync");
	}

	[Fact]
	public async Task DesafectarCurriculaAsync_cierra_el_periodo_de_la_curricula_y_guarda()
	{
		var cursoID = Guid.NewGuid();
		var curricula = SembrarCurricula(cursoID);

		await _servicio.DesafectarCurriculaAsync(new CurriculaRequest(cursoID, curricula.Id));

		curricula.Periodo.FechaFin.ShouldNotBeNull();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task DesafectarCurriculaAsync_con_una_curricula_inexistente_lanza_ArgumentNullException()
	{
		await Should.ThrowAsync<ArgumentNullException>(() => _servicio.DesafectarCurriculaAsync(
			new CurriculaRequest(Guid.NewGuid(), Guid.NewGuid())));
	}
	#endregion
}
