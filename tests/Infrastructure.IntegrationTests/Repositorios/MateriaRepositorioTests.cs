using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

public sealed class MateriaRepositorioTests : BaseIntegracion
{
	public MateriaRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task ExisteNombreMateriaEnCurriculaAsync_es_true_cuando_existe_materia_con_ese_nombre()
	{
		var cursoID = await SembrarCursoAsync();
		var curriculaID = await SembrarCurriculaAsync(cursoID);
		var materia = new MateriaBuilder().ConCurricula(curriculaID).ConDescripcion("Matemática").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(materia);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curriculaID, "Matemática")).ShouldBeTrue();
		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curriculaID, "matemática")).ShouldBeTrue();
		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curriculaID, "Historia")).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task ExisteNombreMateriaEnCurriculaAsync_no_confunde_materias_de_otras_curriculas()
	{
		var cursoID = await SembrarCursoAsync();
		var curricula1ID = await SembrarCurriculaAsync(cursoID);
		var curricula2ID = await SembrarCurriculaAsync(cursoID);
		var materia1 = new MateriaBuilder().ConCurricula(curricula1ID).ConDescripcion("Matemática").Build();
		var materia2 = new MateriaBuilder().ConCurricula(curricula2ID).ConDescripcion("Historia").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(materia1, materia2);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curricula1ID, "Matemática")).ShouldBeTrue();
		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curricula1ID, "Historia")).ShouldBeFalse();
		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curricula2ID, "Historia")).ShouldBeTrue();
		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curricula2ID, "Matemática")).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task ExisteNombreMateriaEnCurriculaAsync_con_excluir_ignora_la_materia_indicada()
	{
		var cursoID = await SembrarCursoAsync();
		var curriculaID = await SembrarCurriculaAsync(cursoID);
		var materia = new MateriaBuilder().ConCurricula(curriculaID).ConDescripcion("Matemática").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(materia);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curriculaID, "Matemática", materia.Id)).ShouldBeFalse();
		var otraMateria = new MateriaBuilder().ConCurricula(curriculaID).ConDescripcion("Historia").Build();
		(await uow.Materias.ExisteNombreMateriaEnCurriculaAsync(curriculaID, "Matemática", otraMateria.Id)).ShouldBeTrue();
	}

	[RequiereSqlServerFact]
	public async Task BuscarMateriasSegunCurriculaAsync_devuelve_todas_las_materias_de_la_curricula()
	{
		var cursoID = await SembrarCursoAsync();
		var curricula1ID = await SembrarCurriculaAsync(cursoID);
		var curricula2ID = await SembrarCurriculaAsync(cursoID);
		var materia1 = new MateriaBuilder().ConCurricula(curricula1ID).ConDescripcion("Matemática").Build();
		var materia2 = new MateriaBuilder().ConCurricula(curricula1ID).ConDescripcion("Historia").Build();
		var materia3 = new MateriaBuilder().ConCurricula(curricula2ID).ConDescripcion("Inglés").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(materia1, materia2, materia3);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var materias1 = await uow.Materias.BuscarMateriasSegunCurriculaAsync(curricula1ID);
		materias1.Count.ShouldBe(2);
		materias1.ShouldAllBe(x => x.CurriculaID == curricula1ID);

		var materias2 = await uow.Materias.BuscarMateriasSegunCurriculaAsync(curricula2ID);
		materias2.Count.ShouldBe(1);
		materias2.ShouldAllBe(x => x.CurriculaID == curricula2ID);
	}

	[RequiereSqlServerFact]
	public async Task TotalHorasCatedraSegunCurriculaAsync_suma_horas_de_todas_las_materias()
	{
		var cursoID = await SembrarCursoAsync();
		var curricula1ID = await SembrarCurriculaAsync(cursoID);
		var curricula2ID = await SembrarCurriculaAsync(cursoID);
		var materia1 = new MateriaBuilder().ConCurricula(curricula1ID).ConHorasCatedra(4).Build();
		var materia2 = new MateriaBuilder().ConCurricula(curricula1ID).ConHorasCatedra(3).Build();
		var materia3 = new MateriaBuilder().ConCurricula(curricula2ID).ConHorasCatedra(5).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(materia1, materia2, materia3);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var horas1 = await uow.Materias.TotalHorasCatedraSegunCurriculaAsync(curricula1ID);
		horas1.ShouldBe(7);

		var horas2 = await uow.Materias.TotalHorasCatedraSegunCurriculaAsync(curricula2ID);
		horas2.ShouldBe(5);
	}

	[RequiereSqlServerFact]
	public async Task TotalEspaciosSegunCurriculaAsync_cuenta_todas_las_materias()
	{
		var cursoID = await SembrarCursoAsync();
		var curricula1ID = await SembrarCurriculaAsync(cursoID);
		var curricula2ID = await SembrarCurriculaAsync(cursoID);
		var materia1 = new MateriaBuilder().ConCurricula(curricula1ID).Build();
		var materia2 = new MateriaBuilder().ConCurricula(curricula1ID).Build();
		var materia3 = new MateriaBuilder().ConCurricula(curricula1ID).Build();
		var materia4 = new MateriaBuilder().ConCurricula(curricula2ID).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(materia1, materia2, materia3, materia4);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var espacios1 = await uow.Materias.TotalEspaciosSegunCurriculaAsync(curricula1ID);
		espacios1.ShouldBe(3);

		var espacios2 = await uow.Materias.TotalEspaciosSegunCurriculaAsync(curricula2ID);
		espacios2.ShouldBe(1);
	}
}
