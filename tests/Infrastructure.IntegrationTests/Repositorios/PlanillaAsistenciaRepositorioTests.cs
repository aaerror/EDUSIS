using Domain.Asistencias;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

public sealed class PlanillaAsistenciaRepositorioTests : BaseIntegracion
{
	public PlanillaAsistenciaRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task ExistePlanillaAsync_es_true_cuando_existe_planilla_de_esa_division_y_fecha()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var hoy = DateTime.Today;
		var cursante1ID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(planilla);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.PlanillasAsistencia.ExistePlanillaAsync(divisionID, hoy)).ShouldBeTrue();
		(await uow.PlanillasAsistencia.ExistePlanillaAsync(divisionID, hoy.AddDays(1))).ShouldBeFalse();
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorDivisionYFechaAsync_devuelve_la_planilla_de_esa_division_y_fecha()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var hoy = DateTime.Today;
		var cursante1ID = Guid.NewGuid();
		var planilla = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(planilla);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var encontrada = await uow.PlanillasAsistencia.BuscarPorDivisionYFechaAsync(divisionID, hoy);
		encontrada.ShouldNotBeNull();
		encontrada.Id.ShouldBe(planilla.Id);

		var noEncontrada = await uow.PlanillasAsistencia.BuscarPorDivisionYFechaAsync(divisionID, hoy.AddDays(1));
		noEncontrada.ShouldBeNull();
	}

	[RequiereSqlServerFact]
	public async Task ContarFaltasAsync_cuenta_faltas_de_un_cursante()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var cursante1ID = Guid.NewGuid();
		var cursante2ID = Guid.NewGuid();
		var hoy = DateTime.Today;

		var planilla1 = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID, cursante2ID)
			.Build();
		planilla1.MarcarAusencia(cursante1ID);

		var planilla2 = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy.AddDays(1))
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID, cursante2ID)
			.Build();
		planilla2.MarcarAusencia(cursante1ID);
		planilla2.MarcarAusencia(cursante2ID);

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(planilla1, planilla2);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var faltas1 = await uow.PlanillasAsistencia.ContarFaltasAsync(cursante1ID, TipoAsistencia.Ausencia);
		faltas1.ShouldBe(2);

		var faltas2 = await uow.PlanillasAsistencia.ContarFaltasAsync(cursante2ID, TipoAsistencia.Ausencia);
		faltas2.ShouldBe(1);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorIDAsync_carga_los_registros_de_asistencia()
	{
		var cursoID = await SembrarCursoAsync();
		var divisionID = await SembrarDivisionAsync(cursoID);
		var preceptorID = await SembrarDocenteAsync();
		var cursante1ID = Guid.NewGuid();
		var hoy = DateTime.Today;
		var planilla = new PlanillaAsistenciaBuilder()
			.ConDivision(divisionID)
			.ConFecha(hoy)
			.ConPreceptor(preceptorID)
			.ConCursantes(cursante1ID)
			.Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(planilla);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var cargada = await uow.PlanillasAsistencia.BuscarPorIDAsync(planilla.Id);
		cargada.ShouldNotBeNull();
		cargada.Registros.ShouldNotBeNull();
		cargada.Registros.Count.ShouldBeGreaterThan(0);
	}
}
