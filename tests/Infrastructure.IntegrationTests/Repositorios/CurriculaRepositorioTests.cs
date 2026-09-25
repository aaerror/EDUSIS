using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// <c>CurriculaRepository</c>: consultas por curso. Las materias son un agregado propio y se
/// verifican en <see cref="MateriaRepositorioTests"/>.
/// </summary>
public sealed class CurriculaRepositorioTests : BaseIntegracion
{
	public CurriculaRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task BuscarCurriculaAsync_devuelve_la_curricula_del_curso_especificado()
	{
		var cursoID = await SembrarCursoAsync();
		var curriculaID = await SembrarCurriculaAsync(cursoID);

		using var uow = CrearUnidadDeTrabajo();

		var curricula = await uow.Curriculas.BuscarCurriculaAsync(cursoID, curriculaID);

		curricula.ShouldNotBeNull();
		curricula.Id.ShouldBe(curriculaID);
		curricula.CursoID.ShouldBe(cursoID);
	}

	[RequiereSqlServerFact]
	public async Task BuscarCurriculaAsync_devuelve_null_si_la_curricula_es_de_otro_curso()
	{
		var cursoID = await SembrarCursoAsync("Primero");
		var otroCursoID = await SembrarCursoAsync("Segundo");
		var curriculaID = await SembrarCurriculaAsync(cursoID);

		using var uow = CrearUnidadDeTrabajo();

		var curricula = await uow.Curriculas.BuscarCurriculaAsync(otroCursoID, curriculaID);

		curricula.ShouldBeNull();
	}

	[RequiereSqlServerFact]
	public async Task CurriculasSegunCursoAsync_devuelve_solo_las_curriculas_del_curso()
	{
		var cursoID = await SembrarCursoAsync("Primero");
		var otroCursoID = await SembrarCursoAsync("Segundo");
		var primeraID = await SembrarCurriculaAsync(cursoID);
		var segundaID = await SembrarCurriculaAsync(cursoID);
		await SembrarCurriculaAsync(otroCursoID);

		using var uow = CrearUnidadDeTrabajo();

		var curriculas = await uow.Curriculas.CurriculasSegunCursoAsync(cursoID);

		curriculas.Select(x => x.Id).ShouldBe(new[] { primeraID, segundaID }, ignoreOrder: true);
	}
}
