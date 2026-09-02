using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// US3-2: <c>LicenciaRepository.BuscarLicenciasDeDocenteAsync</c> devuelve sólo las licencias
/// del docente indicado sobre datos sembrados.
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

		var licenciaA1 = new LicenciaBuilder().ConDocente(docenteA).ConArticulo("Enfermedad").Build();
		var licenciaA2 = new LicenciaBuilder().ConDocente(docenteA).ConArticulo("Particular").Build();
		var licenciaB1 = new LicenciaBuilder().ConDocente(docenteB).ConArticulo("Matrimonio").Build();

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
}
