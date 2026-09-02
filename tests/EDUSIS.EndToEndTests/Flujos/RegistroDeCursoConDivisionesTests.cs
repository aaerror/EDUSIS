using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// Flujo e2e 2 (SC-003 / data-model.md §6): registro de un curso y alta de divisiones desde la
/// fachada <see cref="IServicioCurso"/> hasta SQL Server real. Se verifica el estado persistido
/// del curso y de sus divisiones vía <see cref="IServicioCurso.BuscarDivisionesAsync"/> en
/// <em>scopes</em> nuevos.
/// </summary>
public sealed class RegistroDeCursoConDivisionesTests : BaseE2E
{
	public RegistroDeCursoConDivisionesTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task RegistrarCurso_y_AgregarDivisionAlCurso_persisten_el_curso_con_sus_divisiones()
	{
		await EnUnScope(sp =>
			sp.GetRequiredService<IServicioCurso>().RegistrarCurso(new RegistrarCursoRequest("Primero", "Secundaria")));

		// El curso recién creado se localiza por el listado (RegistrarCurso no devuelve el Id).
		var cursoID = await EnUnScope(async sp =>
		{
			var cursos = await sp.GetRequiredService<IServicioCurso>().ListarCursosAsync();
			var curso = cursos.ShouldHaveSingleItem();
			curso.Grado.ToString().ShouldBe("Primero");
			curso.NivelEducativo.ToString().ShouldBe("Secundaria");
			curso.Divisiones.ShouldBe(0);
			return curso.CursoID;
		});

		// Cada alta de división es un request independiente: su propio scope / su propia UoW.
		await EnUnScope(sp => sp.GetRequiredService<IServicioCurso>().AgregarDivisionAlCurso(cursoID));
		await EnUnScope(sp => sp.GetRequiredService<IServicioCurso>().AgregarDivisionAlCurso(cursoID));

		var divisiones = await EnUnScope(sp =>
			sp.GetRequiredService<IServicioCurso>().BuscarDivisionesAsync(cursoID));

		divisiones.Count.ShouldBe(2);
		divisiones.Select(x => x.Descripcion).OrderBy(x => x).ShouldBe(new[] { "A", "B" });

		// El listado de cursos también refleja las 2 divisiones persistidas.
		var cursosFinal = await EnUnScope(sp =>
			sp.GetRequiredService<IServicioCurso>().ListarCursosAsync());
		cursosFinal.ShouldHaveSingleItem().Divisiones.ShouldBe(2);
	}
}
