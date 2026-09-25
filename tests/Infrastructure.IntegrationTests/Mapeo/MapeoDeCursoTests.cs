using Domain.Cursos;
using Domain.Divisiones;
using Domain.Shared;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// Mapeo de <see cref="Curso"/> y sus enumeraciones.
/// Nota: <c>Division</c> es ahora un agregado propio (ver <see cref="DivisionRepositorioTests"/>).
/// </summary>
public sealed class MapeoDeCursoTests : BaseIntegracion
{
	public MapeoDeCursoTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Un_curso_se_relee_con_sus_atributos_correctos()
	{
		var curso = new CursoBuilder()
			.ConGrado("Primero")
			.ConNivelEducativo("Secundaria")
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(curso);
			await contexto.SaveChangesAsync();
		}

		await using (var contexto = Fixture.CrearContexto())
		{
			var releido = await contexto.Set<Curso>()
				.SingleAsync(x => x.Id == curso.Id);

			releido.Grado.ShouldBe(Grado.Primero);
			releido.NivelEducativo.ShouldBe(NivelEducativo.Secundaria);
		}
	}

	/// <summary>
	/// Defecto H-023: <c>DivisionesConfiguration</c> ya mapea <c>descripcion</c> como
	/// <c>varchar(1)</c>, pero el esquema de prueba se crea con <c>Database.Migrate()</c> y la
	/// migración <c>InitCreate</c> todavía declara la columna de ancho fijo, así que un ida y
	/// vuelta devuelve <c>"A"</c> con relleno de espacios. Se resuelve al regenerar la migración.
	/// Ver <c>hallazgos.md</c>.
	/// </summary>
	[Fact(Skip = "Defecto H-023: Division.Descripcion vuelve con relleno de espacios hasta regenerar la migración; ver hallazgos.md")]
	public async Task La_descripcion_de_la_division_no_deberia_volver_con_relleno_de_espacios()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID, "A");

		await using var contexto = Fixture.CrearContexto();
		var releida = await contexto.Set<Division>()
			.SingleAsync(x => x.Id == divisionID);

		releida.Descripcion.ShouldBe("A");
	}
}
