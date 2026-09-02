using Domain.Cursos;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Mapeo;

/// <summary>
/// US3-1: persistir y releer un <see cref="Curso"/> con divisiones. Verifica el enum
/// <see cref="Grado"/>/<see cref="NivelEducativo"/> y la colección <c>Divisiones</c>
/// (tabla <c>division</c>) con su descripción asignada por el dominio (A, B, …).
/// </summary>
public sealed class MapeoDeCursoTests : BaseIntegracion
{
	public MapeoDeCursoTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task Un_curso_con_dos_divisiones_se_relee_completo()
	{
		var curso = new CursoBuilder()
			.ConGrado("Primero")
			.ConNivelEducativo("Secundaria")
			.ConDivision(2)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(curso);
			await contexto.SaveChangesAsync();
		}

		await using (var contexto = Fixture.CrearContexto())
		{
			var releido = await contexto.Set<Curso>()
				.Include(x => x.Divisiones)
				.SingleAsync(x => x.Id == curso.Id);

			releido.Grado.ShouldBe(Grado.Primero);
			releido.NivelEducativo.ShouldBe(NivelEducativo.Secundaria);
			releido.Divisiones.Count.ShouldBe(2);
			// Defecto H-023: `Descripcion` vuelve con relleno de espacios (columna de ancho
			// fijo). Se compara con TrimEnd(); el valor exacto se verifica —marcado Skip— en
			// La_descripcion_de_la_division_no_deberia_volver_con_relleno_de_espacios.
			releido.Divisiones.Select(x => x.Descripcion.TrimEnd()).OrderBy(x => x)
				.ShouldBe(new[] { "A", "B" });
		}
	}

	/// <summary>
	/// Defecto H-023: la columna de <c>Division.Descripcion</c> (mapeada en
	/// <c>CursosConfiguration.ConfigureTableDivisiones</c> como <c>char(1)</c>) existe en la
	/// base como ancho fijo de 50, así que un ida y vuelta devuelve <c>"A"</c> seguido de 49
	/// espacios en lugar de <c>"A"</c>. Ver <c>hallazgos.md</c>.
	/// </summary>
	[Fact(Skip = "Defecto H-023: Division.Descripcion vuelve con relleno de espacios; ver hallazgos.md")]
	public async Task La_descripcion_de_la_division_no_deberia_volver_con_relleno_de_espacios()
	{
		var curso = new CursoBuilder().ConDivision(1).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(curso);
			await contexto.SaveChangesAsync();
		}

		await using (var contexto = Fixture.CrearContexto())
		{
			var releido = await contexto.Set<Curso>()
				.Include(x => x.Divisiones)
				.SingleAsync(x => x.Id == curso.Id);

			releido.Divisiones.Single().Descripcion.ShouldBe("A");
		}
	}
}
