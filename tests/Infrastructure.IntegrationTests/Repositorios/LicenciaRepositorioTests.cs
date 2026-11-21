using Domain.Shared;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Consultas de <c>LicenciaRepository</c> contra SQL Server real: por docente, pendientes de todos
/// los docentes y superposición de períodos.
/// </summary>
public sealed class LicenciaRepositorioTests : BaseIntegracion
{
	public LicenciaRepositorioTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	[RequiereSqlServerFact]
	public async Task BuscarLicenciasDeDocenteAsync_devuelve_solo_las_licencias_de_ese_docente()
	{
		var docenteA = await SembrarDocenteAsync();
		var docenteB = await SembrarDocenteAsync();

		var licenciaA1 = new LicenciaBuilder().ConDocente(docenteA).ConArticulo("AccidenteDeTrabajo").Build();
		var licenciaA2 = new LicenciaBuilder().ConDocente(docenteA).ConArticulo("OtrasCausas").Build();
		var licenciaB1 = new LicenciaBuilder().ConDocente(docenteB).ConArticulo("EstudioEncomendado").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(licenciaA1, licenciaA2, licenciaB1);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var deA = await uow.Licencias.BuscarLicenciasDeDocenteAsync(docenteA);
		var deB = await uow.Licencias.BuscarLicenciasDeDocenteAsync(docenteB);

		deA.Count.ShouldBe(2);
		deA.ShouldAllBe(x => x.DocenteID == docenteA);
		deB.Count.ShouldBe(1);
	}

	[RequiereSqlServerFact]
	public async Task BuscarPorIDYDocenteAsync_devuelve_la_licencia_solo_si_el_docente_coincide()
	{
		var docenteA = await SembrarDocenteAsync();
		var docenteB = await SembrarDocenteAsync();

		var licencia = new LicenciaBuilder().ConDocente(docenteA).ConArticulo("AccidenteDeTrabajo").Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.Add(licencia);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var encontrada = await uow.Licencias.BuscarPorIDYDocenteAsync(licencia.Id, docenteA);
		var noEncontrada = await uow.Licencias.BuscarPorIDYDocenteAsync(licencia.Id, docenteB);

		encontrada.ShouldNotBeNull();
		encontrada!.Id.ShouldBe(licencia.Id);
		noEncontrada.ShouldBeNull();
	}

	/// <remarks>
	/// La traducción a SQL de <c>Licencia.SeSuperponeCon</c>: los dos extremos son días de licencia,
	/// un fin nulo no termina, la rechazada no cuenta y la licencia excluida no choca consigo misma.
	/// Los mismos casos que el dominio, pero contra la consulta que corre EF.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task ExisteSuperpuestaAsync_traduce_la_regla_de_superposicion_del_dominio()
	{
		var docente = await SembrarDocenteAsync();
		var otroDocente = await SembrarDocenteAsync();
		var marzo = new DateTime(2026, 3, 1);

		var delUnoAlQuince = new LicenciaBuilder().ConDocente(docente).ConFechaInicio(marzo).ConFechaFin(marzo.AddDays(14)).Build();
		var rechazada = new LicenciaBuilder().ConDocente(docente).ConFechaInicio(marzo.AddDays(30)).ConFechaFin(marzo.AddDays(40)).Build();
		rechazada.RechazarLicencia(null);
		var indefinida = new LicenciaBuilder().ConDocente(otroDocente).ConFechaInicio(marzo).Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(delUnoAlQuince, rechazada, indefinida);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		(await uow.Licencias.ExisteSuperpuestaAsync(docente, RangoFechas.Create(marzo.AddDays(14), marzo.AddDays(20)), Guid.Empty)).ShouldBeTrue();
		(await uow.Licencias.ExisteSuperpuestaAsync(docente, RangoFechas.Create(marzo.AddDays(15), marzo.AddDays(20)), Guid.Empty)).ShouldBeFalse();
		(await uow.Licencias.ExisteSuperpuestaAsync(docente, RangoFechas.Create(marzo.AddDays(-30)), Guid.Empty)).ShouldBeTrue();
		(await uow.Licencias.ExisteSuperpuestaAsync(docente, RangoFechas.Create(marzo.AddDays(32), marzo.AddDays(35)), Guid.Empty)).ShouldBeFalse();
		(await uow.Licencias.ExisteSuperpuestaAsync(docente, RangoFechas.Create(marzo, marzo.AddDays(3)), delUnoAlQuince.Id)).ShouldBeFalse();
		(await uow.Licencias.ExisteSuperpuestaAsync(otroDocente, RangoFechas.Create(new DateTime(2030, 1, 1), new DateTime(2030, 1, 2)), Guid.Empty)).ShouldBeTrue();
	}

	[RequiereSqlServerFact]
	public async Task BuscarPendientesAsync_devuelve_solo_las_pendientes_de_todos_los_docentes()
	{
		var docenteA = await SembrarDocenteAsync();
		var docenteB = await SembrarDocenteAsync();

		var pendienteA = new LicenciaBuilder().ConDocente(docenteA).Build();
		var pendienteB = new LicenciaBuilder().ConDocente(docenteB).ConFechaInicio(DateTime.Today.AddDays(-3)).Build();
		var aprobada = new LicenciaBuilder().ConDocente(docenteA).ConFechaInicio(DateTime.Today.AddDays(-30)).ConFechaFin(DateTime.Today.AddDays(-20)).Aprobada().Build();

		await using (var contexto = Fixture.CrearContexto())
		{
			contexto.AddRange(pendienteA, pendienteB, aprobada);
			await contexto.SaveChangesAsync();
		}

		using var uow = CrearUnidadDeTrabajo();

		var pendientes = await uow.Licencias.BuscarPendientesAsync();

		pendientes.Select(x => x.Id).ShouldBe(new[] { pendienteB.Id, pendienteA.Id });
	}
}
