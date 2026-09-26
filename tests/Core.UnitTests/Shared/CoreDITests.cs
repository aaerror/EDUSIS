using Core.ServicioAsistencias;
using Core.ServicioCatedras;
using Core.ServicioCursantes;
using Core.ServicioDivisiones;
using Core.ServicioMaterias;
using Core.UnitTests.Infraestructura;
using EDUSIS.TestSupport;
using Shouldly;
using Xunit;

namespace Core.UnitTests.Shared;

/// <summary>
/// Prueba de humo de <see cref="Core.Shared.CoreDI.AddServices"/>: verifica que los cinco
/// servicios nuevos de la fusión de agregados (<c>Division</c>, <c>Cursante</c>,
/// <c>Materia</c>, <c>Catedra</c>, <c>PlanillaAsistencia</c>) se resuelven desde el
/// contenedor real, igual que los nueve preexistentes que cada suite propia ya ejercita
/// al construirse.
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class CoreDITests
{
	[Fact]
	public void AddServices_resuelve_los_cinco_servicios_nuevos_de_la_fusion_de_agregados()
	{
		using var host = new HostDeServicios();

		host.Resolver<IServicioDivision>().ShouldNotBeNull();
		host.Resolver<IServicioCursante>().ShouldNotBeNull();
		host.Resolver<IServicioMateria>().ShouldNotBeNull();
		host.Resolver<IServicioCatedra>().ShouldNotBeNull();
		host.Resolver<IServicioAsistencia>().ShouldNotBeNull();
	}
}
