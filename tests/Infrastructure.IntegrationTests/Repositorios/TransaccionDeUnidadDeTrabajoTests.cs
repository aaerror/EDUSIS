using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure.IntegrationTests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Repositorios;

/// <summary>
/// Ciclo de transacción de <see cref="UnitOfWork"/> contra SQL Server real.
/// </summary>
public sealed class TransaccionDeUnidadDeTrabajoTests : BaseIntegracion
{
	public TransaccionDeUnidadDeTrabajoTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	/// <remarks>
	/// El rollback revertía la base pero dejaba el <c>ChangeTracker</c> como estaba: lo guardado
	/// dentro de la transacción seguía rastreado como si existiera y lo pendiente seguía
	/// <c>Added</c>. Con un contexto por pantalla, el siguiente guardado lo reinsertaba.
	/// </remarks>
	[RequiereSqlServerFact]
	public async Task RollbackTransactionAsync_revierte_la_base_y_limpia_el_ChangeTracker()
	{
		var docenteID = await SembrarDocenteAsync();
		await using var contexto = Fixture.CrearContexto();
		using var unidad = new UnitOfWork(contexto, CrearMediator());

		await unidad.BeginTransactionAsync();
		var guardada = new LicenciaBuilder().ConDocente(docenteID).Build();
		await unidad.Licencias.AgregarAsync(guardada);
		await unidad.GuardarCambiosAsync();
		var pendiente = new LicenciaBuilder().ConDocente(docenteID).Build();
		await unidad.Licencias.AgregarAsync(pendiente);

		await unidad.RollbackTransactionAsync();

		contexto.ChangeTracker.Entries().ShouldBeEmpty();
		await unidad.GuardarCambiosAsync();

		await using var verificacion = Fixture.CrearContexto();
		(await verificacion.Set<Domain.Licencias.Licencia>().AnyAsync(x => x.DocenteID == docenteID)).ShouldBeFalse();
	}
}
