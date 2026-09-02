using Domain.Cursos;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// US3-2: consultas de <c>CursoRepository</c> con <c>Include(x =&gt; x.Divisiones)</c> y
/// <c>SelectMany</c> sobre datos sembrados.
/// </summary>
public sealed class CursoRepositorioTests : BaseIntegracion
{
	public CursoRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private async Task<Curso> SembrarCursoAsync(string grado, int divisiones)
	{
		var curso = new CursoBuilder()
			.ConGrado(grado)
			.ConNivelEducativo("Secundaria")
			.ConDivision(divisiones)
			.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(curso);
		await contexto.SaveChangesAsync();
		return curso;
	}

	[RequiereSqlServerFact]
	public async Task CursoConDivisiones_trae_el_curso_con_sus_divisiones()
	{
		var sembrado = await SembrarCursoAsync("Primero", 3);

		using var uow = CrearUnidadDeTrabajo();

		var curso = uow.Cursos.CursoConDivisiones(sembrado.Id);

		curso.ShouldNotBeNull();
		curso!.Divisiones.Count.ShouldBe(3);
	}

	[RequiereSqlServerFact]
	public async Task DivisionesDelCurso_devuelve_solo_las_divisiones_de_ese_curso()
	{
		var sembrado = await SembrarCursoAsync("Segundo", 2);
		await SembrarCursoAsync("Tercero", 4);

		using var uow = CrearUnidadDeTrabajo();

		var divisiones = uow.Cursos.DivisionesDelCurso(sembrado.Id).ToList();

		divisiones.Count.ShouldBe(2);
		// Descripcion vuelve con relleno de espacios (defecto H-023): se compara con TrimEnd().
		divisiones.Select(x => x.Descripcion.TrimEnd()).OrderBy(x => x).ShouldBe(new[] { "A", "B" });
	}

	[RequiereSqlServerFact]
	public async Task CursosConDivisiones_incluye_las_divisiones_de_todos_los_cursos()
	{
		await SembrarCursoAsync("Cuarto", 1);
		await SembrarCursoAsync("Quinto", 2);

		using var uow = CrearUnidadDeTrabajo();

		var cursos = uow.Cursos.CursosConDivisiones().ToList();

		cursos.Count.ShouldBe(2);
		cursos.Sum(x => x.Divisiones.Count).ShouldBe(3);
	}
}
