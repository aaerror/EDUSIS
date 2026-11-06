using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

public sealed class DivisionRepositorioTests : BaseIntegracion
{
	public DivisionRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task ExisteDivisionConDescripcionAsync_es_true_cuando_existe_division_con_esa_descripcion()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionA = new DivisionBuilder().ConCurso(cursoID).ConDescripcion("A").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(divisionA);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Divisiones.ExisteDivisionConDescripcionAsync(cursoID, "A")).ShouldBeTrue();
		(await uow.Divisiones.ExisteDivisionConDescripcionAsync(cursoID, "B")).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task ExisteDivisionConDescripcionAsync_no_confunde_divisiones_de_otros_cursos()
	{
		var cursoA = await SembrarCursoAsync("Primero");
		var cursoB = await SembrarCursoAsync("Segundo");
		var divisionACursoA = new DivisionBuilder().ConCurso(cursoA).ConDescripcion("A").Build();
		var divisionACursoB = new DivisionBuilder().ConCurso(cursoB).ConDescripcion("A").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(divisionACursoA, divisionACursoB);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Divisiones.ExisteDivisionConDescripcionAsync(cursoA, "A")).ShouldBeTrue();
		(await uow.Divisiones.ExisteDivisionConDescripcionAsync(cursoB, "A")).ShouldBeTrue();
		(await uow.Divisiones.ExisteDivisionConDescripcionAsync(cursoA, "B")).ShouldBeFalse();
		(await uow.Divisiones.ExisteDivisionConDescripcionAsync(cursoB, "B")).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task ExistePreceptorAsignadoAsync_es_true_cuando_el_docente_es_preceptor()
	{
		var docenteID = await SembrarDocenteAsync();
		var cursoID = await SembrarCursoAsync();
		var division = new DivisionBuilder().ConCurso(cursoID).ConDescripcion("A").Build();
		division.AsignarPreceptor(docenteID);

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(division);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Divisiones.ExistePreceptorAsignadoAsync(docenteID)).ShouldBeTrue();
		var otroDocente = await SembrarDocenteAsync();
		(await uow.Divisiones.ExistePreceptorAsignadoAsync(otroDocente)).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task DescripcionesDelCursoAsync_devuelve_solo_las_divisiones_de_ese_curso()
	{
		var cursoA = await SembrarCursoAsync("Primero");
		var cursoB = await SembrarCursoAsync("Segundo");
		var divisionA1 = new DivisionBuilder().ConCurso(cursoA).ConDescripcion("A").Build();
		var divisionA2 = new DivisionBuilder().ConCurso(cursoA).ConDescripcion("B").Build();
		var divisionB1 = new DivisionBuilder().ConCurso(cursoB).ConDescripcion("A").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(divisionA1, divisionA2, divisionB1);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var descripcionesA = await uow.Divisiones.DescripcionesDelCursoAsync(cursoA);
		descripcionesA.Count.ShouldBe(2);
		descripcionesA.ShouldContain("A");
		descripcionesA.ShouldContain("B");

		var descripcionesB = await uow.Divisiones.DescripcionesDelCursoAsync(cursoB);
		descripcionesB.Count.ShouldBe(1);
		descripcionesB.ShouldContain("A");
	}

	[RequiereSqlServerFact]
	public async Task DivisionesDelCursoAsync_devuelve_todas_las_divisiones_del_curso()
	{
		var cursoA = await SembrarCursoAsync("Primero");
		var cursoB = await SembrarCursoAsync("Segundo");
		var divisionA1 = new DivisionBuilder().ConCurso(cursoA).ConDescripcion("A").Build();
		var divisionA2 = new DivisionBuilder().ConCurso(cursoA).ConDescripcion("B").Build();
		var divisionB1 = new DivisionBuilder().ConCurso(cursoB).ConDescripcion("A").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(divisionA1, divisionA2, divisionB1);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var divisionsA = await uow.Divisiones.DivisionesDelCursoAsync(cursoA);
		divisionsA.Count.ShouldBe(2);
		divisionsA.ShouldAllBe(x => x.CursoID == cursoA);

		var divisionsB = await uow.Divisiones.DivisionesDelCursoAsync(cursoB);
		divisionsB.Count.ShouldBe(1);
		divisionsB.ShouldAllBe(x => x.CursoID == cursoB);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorCursoYDescripcionAsync_devuelve_la_division_con_ese_curso_y_descripcion()
	{
		var cursoA = await SembrarCursoAsync("Primero");
		var cursoB = await SembrarCursoAsync("Segundo");
		var divisionA = new DivisionBuilder().ConCurso(cursoA).ConDescripcion("A").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(divisionA);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var encontrada = await uow.Divisiones.BuscarPorCursoYDescripcionAsync(cursoA, "A");
		encontrada.ShouldNotBeNull();
		encontrada.Id.ShouldBe(divisionA.Id);

		var noEncontrada = await uow.Divisiones.BuscarPorCursoYDescripcionAsync(cursoB, "A");
		noEncontrada.ShouldBeNull();
	}

	/// <remarks>
	/// La consume el borrado del docente, que necesita las instancias para vaciarles el cargo con
	/// <c>Division.QuitarPreceptor()</c>. Afirma además que coincide con
	/// <c>ExistePreceptorAsignadoAsync</c>, que es la invariante que las mantiene alineadas: si una
	/// filtrara distinto de la otra, el borrado dejaría una división con un <c>preceptor_id</c>
	/// apuntando a un legajo inexistente — y ahí no hay clave ajena que lo frene, porque
	/// <c>FK_DIVISION_DOCENTE</c> sigue en <c>Restrict</c> y el borrado pasaría igual por haber
	/// vaciado sólo algunas.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task BuscarPorPreceptorAsync_trae_las_divisiones_del_docente_y_no_las_de_otro()
	{
		var cursoID = await SembrarCursoAsync();
		var preceptorID = await SembrarDocenteAsync();
		var otroDocenteID = await SembrarDocenteAsync();

		var divisionID = await SembrarDivisionAsync(cursoID, "A");
		var otraDivisionID = await SembrarDivisionAsync(cursoID, "B");
		var sinPreceptorID = await SembrarDivisionAsync(cursoID, "C");

		await using (var contexto = Fixture.CrearContexto())
		{
			var division = await contexto.Divisiones.FindAsync(divisionID);
			division!.AsignarPreceptor(preceptorID);

			var otra = await contexto.Divisiones.FindAsync(otraDivisionID);
			otra!.AsignarPreceptor(otroDocenteID);

			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var suyas = await uow.Divisiones.BuscarPorPreceptorAsync(preceptorID);

		suyas.ShouldHaveSingleItem().Id.ShouldBe(divisionID);
		(await uow.Divisiones.ExistePreceptorAsignadoAsync(preceptorID)).ShouldBe(suyas.Count > 0);

		// La división sin preceptor no aparece para nadie: el filtro es por igualdad contra una
		// columna anulable, y un NULL no iguala a ningún Guid.
		(await uow.Divisiones.BuscarPorPreceptorAsync(Guid.NewGuid())).ShouldBeEmpty();
		sinPreceptorID.ShouldNotBe(Guid.Empty);
	}
}
