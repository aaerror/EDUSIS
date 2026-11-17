using Domain.Catedras;
using Domain.Catedras.Horarios;
using Domain.Catedras.SituacionesRevista;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

public sealed class CatedraRepositorioTests : BaseIntegracion
{
	public CatedraRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	private async Task<(Guid CursoID, Guid CurriculaID, Guid DivisionID)> ArmarCursoConCurriculaYDivisionAsync()
	{
		var cursoID = await SembrarCursoAsync();
		var curriculaID = await SembrarCurriculaAsync(cursoID);
		var divisionID = await SembrarDivisionAsync(cursoID);
		return (cursoID, curriculaID, divisionID);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorMateriaYDivisionAsync_devuelve_la_catedra_con_esa_materia_y_division()
	{
		var (_, curriculaID, divisionID) = await ArmarCursoConCurriculaYDivisionAsync();
		var materiaID = await SembrarMateriaAsync(curriculaID);
		var catedra = new CatedraBuilder().ConMateria(materiaID).ConDivision(divisionID).ConCargaHoraria(4).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(catedra);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var encontrada = await uow.Catedras.BuscarPorMateriaYDivisionAsync(materiaID, divisionID);
		encontrada.ShouldNotBeNull();
		encontrada.Id.ShouldBe(catedra.Id);

		var noEncontrada = await uow.Catedras.BuscarPorMateriaYDivisionAsync(materiaID, Guid.NewGuid());
		noEncontrada.ShouldBeNull();
	}

	[RequiereSqlServerFact]
	public async Task CatedrasSegunMateriaAsync_devuelve_todas_las_catedras_de_esa_materia()
	{
		var (cursoID, curriculaID, divisionID) = await ArmarCursoConCurriculaYDivisionAsync();
		var division1ID = await SembrarDivisionAsync(cursoID, "B");
		var division2ID = await SembrarDivisionAsync(cursoID, "C");
		var materia1ID = await SembrarMateriaAsync(curriculaID, "Matemática");
		var materia2ID = await SembrarMateriaAsync(curriculaID, "Historia");

		var catedra1 = new CatedraBuilder().ConMateria(materia1ID).ConDivision(division1ID).Build();
		var catedra2 = new CatedraBuilder().ConMateria(materia1ID).ConDivision(division2ID).Build();
		var catedra3 = new CatedraBuilder().ConMateria(materia2ID).ConDivision(division1ID).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(catedra1, catedra2, catedra3);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var catedrasMateria1 = await uow.Catedras.CatedrasSegunMateriaAsync(materia1ID);
		catedrasMateria1.Count.ShouldBe(2);
		catedrasMateria1.ShouldAllBe(x => x.MateriaID == materia1ID);

		var catedrasMateria2 = await uow.Catedras.CatedrasSegunMateriaAsync(materia2ID);
		catedrasMateria2.Count.ShouldBe(1);
	}

	[RequiereSqlServerFact]
	public async Task CatedrasSegunDivisionAsync_devuelve_todas_las_catedras_de_esa_division()
	{
		var (cursoID, curriculaID, _) = await ArmarCursoConCurriculaYDivisionAsync();
		var division1ID = await SembrarDivisionAsync(cursoID, "B");
		var division2ID = await SembrarDivisionAsync(cursoID, "C");
		var materia1ID = await SembrarMateriaAsync(curriculaID, "Matemática");
		var materia2ID = await SembrarMateriaAsync(curriculaID, "Historia");

		var catedra1 = new CatedraBuilder().ConMateria(materia1ID).ConDivision(division1ID).Build();
		catedra1.AgregarHorario(new HorarioBuilder().Build());
		var catedra2 = new CatedraBuilder().ConMateria(materia2ID).ConDivision(division1ID).Build();
		var catedra3 = new CatedraBuilder().ConMateria(materia1ID).ConDivision(division2ID).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(catedra1, catedra2, catedra3);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var catedrasDiv1 = await uow.Catedras.CatedrasSegunDivisionAsync(division1ID);
		catedrasDiv1.Count.ShouldBe(2);
		catedrasDiv1.ShouldAllBe(x => x.DivisionID == division1ID);

		// Los horarios tienen que venir cargados: `ServicioCatedra.AgregarHorarioAsync` recorre
		// `otra.Horarios` en memoria para detectar la colisión dentro de la división, así que una
		// colección vacía no falla, deja pasar el horario superpuesto.
		// Lo que esta aserción NO acredita: que el `.Include(x => x.Horarios)` de `Consulta()` haga
		// falta. `Horarios` es `OwnsMany` (`CatedrasConfiguration.cs:76`) y EF carga los owned types
		// con su dueño, así que ese `Include` es redundante y borrarlo no la pone en rojo. Cubre el
		// caso de que `Horario` deje de ser owned —pasar a `HasMany` es un refactor plausible—, que
		// es cuando la carga automática desaparece sin que nadie lo note.
		catedrasDiv1.Single(x => x.MateriaID == materia1ID).Horarios.ShouldNotBeEmpty();

		var catedrasDiv2 = await uow.Catedras.CatedrasSegunDivisionAsync(division2ID);
		catedrasDiv2.Count.ShouldBe(1);
	}

	[RequiereSqlServerFact]
	public async Task CatedrasSegunDocenteAsync_devuelve_catedras_donde_el_docente_esta_en_situacion_revista()
	{
		var (_, curriculaID, divisionID) = await ArmarCursoConCurriculaYDivisionAsync();
		var docente1ID = await SembrarDocenteAsync();
		var docente2ID = await SembrarDocenteAsync();
		var materia1ID = await SembrarMateriaAsync(curriculaID, "Matemática");
		var materia2ID = await SembrarMateriaAsync(curriculaID, "Historia");

		var catedra1 = new CatedraBuilder().ConMateria(materia1ID).ConDivision(divisionID).Build();
		var catedra2 = new CatedraBuilder().ConMateria(materia2ID).ConDivision(divisionID).Build();
		catedra1.Designar(docente1ID, Cargo.Titular, DateTime.Today, null);
		catedra2.Designar(docente2ID, Cargo.Titular, DateTime.Today, null);

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(catedra1, catedra2);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var catedrasDocente1 = await uow.Catedras.CatedrasSegunDocenteAsync(docente1ID);
		catedrasDocente1.Count.ShouldBe(1);
		catedrasDocente1.First().MateriaID.ShouldBe(materia1ID);

		// Las situaciones de revista tienen que venir CARGADAS, no sólo filtradas, y las dos
		// aserciones de arriba no lo acreditan: el `Where` de la consulta usa
		// `SituacionesRevista.Any(…)`, que EF traduce a SQL del lado del servidor y resuelve sin
		// `Include`, así que `Count` y `MateriaID` dan bien con la colección vacía. Quien la lee en
		// memoria es `Catedra.DocenteEnFunciones()`, y de ella cuelga el invariante
		// `DocenteEnDosAulasException` de `ServicioCatedra.AgregarHorarioAsync`.
		// El `.Include(x => x.SituacionesRevista)` de `Consulta()` ya está fijado por
		// `BuscarPorIDAsync_carga_horarios_y_situaciones_revista`, así que esta aserción no es la
		// única que ve esa mutación: lo que agrega es fijarlo en LA consulta que usa el invariante.
		// `BuscarPorIDAsync` y `CatedrasSegunDocenteAsync` comparten `Consulta()` hoy, pero nada
		// obliga a que sigan compartiéndola, y la de acá es la que alimenta la regla.
		catedrasDocente1.First().SituacionesRevista.ShouldNotBeEmpty();

		var catedrasDocente2 = await uow.Catedras.CatedrasSegunDocenteAsync(docente2ID);
		catedrasDocente2.Count.ShouldBe(1);
		catedrasDocente2.First().MateriaID.ShouldBe(materia2ID);

		var otroDocente = await SembrarDocenteAsync();
		var catedrasOtro = await uow.Catedras.CatedrasSegunDocenteAsync(otroDocente);
		catedrasOtro.Count.ShouldBe(0);
	}

	[RequiereSqlServerFact]
	public async Task ExisteCatedraAsync_es_true_cuando_existe_catedra_con_esa_materia_y_division()
	{
		var (_, curriculaID, divisionID) = await ArmarCursoConCurriculaYDivisionAsync();
		var materiaID = await SembrarMateriaAsync(curriculaID);
		var catedra = new CatedraBuilder().ConMateria(materiaID).ConDivision(divisionID).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(catedra);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Catedras.ExisteCatedraAsync(materiaID, divisionID)).ShouldBeTrue();
		(await uow.Catedras.ExisteCatedraAsync(materiaID, Guid.NewGuid())).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorIDAsync_carga_horarios_y_situaciones_revista()
	{
		var (_, curriculaID, divisionID) = await ArmarCursoConCurriculaYDivisionAsync();
		var docente1ID = await SembrarDocenteAsync();
		var materiaID = await SembrarMateriaAsync(curriculaID);

		var catedra = new CatedraBuilder().ConMateria(materiaID).ConDivision(divisionID).ConCargaHoraria(4).Build();
		catedra.Designar(docente1ID, Cargo.Titular, DateTime.Today, null);

		var horario = new HorarioBuilder().Build();
		catedra.AgregarHorario(horario);

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(catedra);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var cargada = await uow.Catedras.BuscarPorIDAsync(catedra.Id);
		cargada.ShouldNotBeNull();
		cargada.Horarios.Count.ShouldBe(1);
		cargada.SituacionesRevista.Count.ShouldBe(1);
	}
}
