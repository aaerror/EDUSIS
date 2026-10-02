using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using EDUSIS.EndToEndTests.Infraestructura;
using EDUSIS.TestSupport.Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EDUSIS.EndToEndTests.Flujos;

/// <summary>
/// Flujo e2e 2 (SC-003 / data-model.md §6): registro de un curso y alta de divisiones desde la
/// fachada <see cref="IServicioCurso"/> y <see cref="IServicioDivision"/> hasta SQL Server real.
/// Las divisiones son un agregado propio: se verifica el estado persistido del curso y de sus
/// divisiones vía <see cref="IServicioDivision.ListarDivisionesAsync"/> en <em>scopes</em> nuevos.
/// </summary>
public sealed class RegistroDeCursoConDivisionesTests : BaseE2E
{
	public RegistroDeCursoConDivisionesTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task RegistrarCurso_y_AgregarDivision_persisten_el_curso_con_sus_divisiones()
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
			return curso.CursoID;
		});

		var cicloLectivo = DateTime.Now.Year.ToString();

		// Un curso recién registrado no tiene divisiones.
		var divisionesIniciales = await EnUnScope(sp =>
			sp.GetRequiredService<IServicioDivision>().ListarDivisionesAsync(new ListarDivisionesRequest(cursoID, cicloLectivo)));
		divisionesIniciales.ShouldBeEmpty();

		// Cada alta de división es un request independiente: su propio scope / su propia UoW.
		await EnUnScope(sp => sp.GetRequiredService<IServicioDivision>().AgregarDivisionAsync(new AgregarDivisionRequest(cursoID)));
		await EnUnScope(sp => sp.GetRequiredService<IServicioDivision>().AgregarDivisionAsync(new AgregarDivisionRequest(cursoID)));

		var divisiones = await EnUnScope(sp =>
			sp.GetRequiredService<IServicioDivision>().ListarDivisionesAsync(new ListarDivisionesRequest(cursoID, cicloLectivo)));

		divisiones.Count.ShouldBe(2);
		divisiones.Select(x => x.Descripcion).OrderBy(x => x).ShouldBe(new[] { "A", "B" });

		// El curso sigue siendo el único y no guarda sus divisiones: las lleva el agregado Division.
		var cursosFinal = await EnUnScope(sp =>
			sp.GetRequiredService<IServicioCurso>().ListarCursosAsync());
		cursosFinal.ShouldHaveSingleItem().CursoID.ShouldBe(cursoID);
	}
}
