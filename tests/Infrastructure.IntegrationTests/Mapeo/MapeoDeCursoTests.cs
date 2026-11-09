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
	/// Fija el tipo de <c>descripcion</c> contra la base: la letra de la división vuelve sin
	/// relleno de espacios, o sea que la columna es de ancho variable y no fijo.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Cubría el defecto H-023 —<c>DivisionesConfiguration</c> ya mapeaba <c>descripcion</c> como
	/// <c>varchar(1)</c>, pero el esquema de prueba se crea con <c>Database.Migrate()</c> y la
	/// migración <c>InitCreate</c> declaraba la columna de ancho fijo, así que un ida y vuelta
	/// devolvía <c>"A"</c> con relleno— y llevaba <c>[Fact(Skip=…)]</c> con la condición de salida
	/// escrita en el mensaje: "hasta regenerar la migración".
	/// </para>
	/// <para>
	/// <strong>Esa condición se cumplió el 2026-10-11</strong>, cuando la migración se regeneró
	/// desde cero (<c>20261011000305_InitCreate</c>, cierre de H-023), pero el <c>Skip</c> se quedó
	/// tres semanas más. Se retiró el 2026-11-09 y la prueba pasa contra SQL Server real;
	/// <c>DivisionesConfiguration.cs:33</c> mapea <c>HasMaxLength(1).IsUnicode(false)</c>. Detalle
	/// en <c>docs/todos-historial.md</c>, nota del 2026-11-09 (segunda).
	/// </para>
	/// </remarks>
	[RequiereSqlServerFact]
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
