using Domain.Cursos;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// <c>CursoRepository</c>: operaciones heredadas de <c>Repository&lt;Curso&gt;</c>. Las divisiones
/// son un agregado propio y se verifican en <see cref="DivisionRepositorioTests"/>.
/// </summary>
public sealed class CursoRepositorioTests : BaseIntegracion
{
	public CursoRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorIDAsync_trae_el_curso_por_su_id()
	{
		var cursoID = await SembrarCursoAsync("Primero");

		using var uow = CrearUnidadDeTrabajo();

		var curso = await uow.Cursos.BuscarPorIDAsync(cursoID);

		curso.ShouldNotBeNull();
		curso.Id.ShouldBe(cursoID);
		curso.Grado.ShouldBe(Grado.Primero);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorIDAsync_devuelve_null_si_el_curso_no_existe()
	{
		using var uow = CrearUnidadDeTrabajo();

		var curso = await uow.Cursos.BuscarPorIDAsync(Guid.NewGuid());

		curso.ShouldBeNull();
	}

	[RequiereSqlServerFact]
	public async Task BuscarTodosAsync_devuelve_todos_los_cursos()
	{
		var segundoID = await SembrarCursoAsync("Segundo");
		var terceroID = await SembrarCursoAsync("Tercero");

		using var uow = CrearUnidadDeTrabajo();

		var cursos = await uow.Cursos.BuscarTodosAsync();

		cursos.Select(x => x.Id).ShouldBe(new[] { segundoID, terceroID }, ignoreOrder: true);
	}

	[RequiereSqlServerFact]
	public async Task EliminarAsync_borra_el_curso_al_guardar_cambios()
	{
		var cursoID = await SembrarCursoAsync();

		using (var uow = CrearUnidadDeTrabajo())
		{
			await uow.Cursos.EliminarAsync(cursoID);
			await uow.GuardarCambiosAsync();
		}

		using var verificacion = CrearUnidadDeTrabajo();
		(await verificacion.Cursos.BuscarPorIDAsync(cursoID)).ShouldBeNull();
	}
}
