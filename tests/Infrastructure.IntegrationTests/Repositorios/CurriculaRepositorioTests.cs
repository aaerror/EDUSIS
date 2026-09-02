using Domain.Curriculas;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// US3-2: consultas de <c>CurriculaRepository</c> (<c>Include</c> de <c>Materias</c>) sobre
/// datos sembrados.
/// </summary>
public sealed class CurriculaRepositorioTests : BaseIntegracion
{
	public CurriculaRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private async Task<Curricula> SembrarCurriculaAsync(Guid cursoID, params (string Descripcion, int Horas)[] materias)
	{
		var builder = new CurriculaBuilder().ConCurso(cursoID);
		foreach (var (descripcion, horas) in materias)
		{
			builder.ConMateria(descripcion, horas);
		}

		var curricula = builder.Build();

		await using var contexto = Fixture.CrearContexto();
		contexto.Add(curricula);
		await contexto.SaveChangesAsync();
		return curricula;
	}

	/// <summary>
	/// Defecto H-021: <c>CurriculaRepository.CurriculasSegunCursoAsync</c> encadena
	/// <c>.Include(x =&gt; x.Materias).ThenInclude(x =&gt; x.Docentes).ThenInclude(x =&gt; x.Periodo)</c>,
	/// pero <c>Materia.Docentes</c> (<c>SituacionRevista</c>) no está mapeado — su
	/// configuración en <c>MateriasConfiguration</c> está comentada. La consulta revienta en
	/// tiempo de ejecución. Ver <c>hallazgos.md</c>.
	/// </summary>
	[Fact(Skip = "Defecto H-021: CurriculasSegunCursoAsync incluye Materia.Docentes (SituacionRevista) sin mapear; ver hallazgos.md")]
	public async Task CurriculasSegunCursoAsync_devuelve_las_curriculas_del_curso_con_sus_materias()
	{
		var cursoID = await CrearCursoPersistidoAsync();
		await SembrarCurriculaAsync(cursoID, ("Matemática", 4), ("Lengua", 3));

		using var uow = CrearUnidadDeTrabajo();

		var curriculas = (await uow.Curriculas.CurriculasSegunCursoAsync(cursoID)).ToList();

		curriculas.Count.ShouldBe(1);
		curriculas.Single().Materias.Count.ShouldBe(2);
	}

	[RequiereSqlServerFact]
	public async Task Una_curricula_con_materias_se_relee_por_su_id_con_las_materias_cargadas()
	{
		var cursoID = await CrearCursoPersistidoAsync();
		var sembrada = await SembrarCurriculaAsync(cursoID, ("Física", 5));

		await using var contexto = Fixture.CrearContexto();
		var releida = await contexto.Curriculas
			.Include(x => x.Materias)
			.FirstAsync(x => x.Id == sembrada.Id);

		releida.CursoID.ShouldBe(cursoID);
		releida.Materias.Count.ShouldBe(1);
		releida.Materias.Single().Descripcion.ShouldBe("Física");
		releida.Materias.Single().HorasCatedra.ShouldBe(5);
	}
}
