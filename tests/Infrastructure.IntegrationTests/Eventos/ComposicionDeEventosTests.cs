using Domain.Licencias.DomainEvents;
using Domain.Materias.DomainEvents;
using EDUSIS.TestSupport;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Eventos;

/// <summary>
/// FR-018: la composición real de <c>InfrastructureDI</c> registra los
/// <c>INotificationHandler</c> definidos en <c>Core</c>.
/// <para>
/// No hereda de <see cref="Infraestructura.BaseIntegracion"/> a propósito: sólo arma el
/// contenedor, no toca la base. Dentro de esa clase el <c>InitializeAsync</c> del fixture
/// cortaría la prueba en cualquier máquina sin Docker, que es justo donde más falta hace
/// que corra.
/// </summary>
[Trait("Categoria", Categorias.Integracion)]
public sealed class ComposicionDeEventosTests
{
	/// <summary>
	/// <c>AddInfrastructure</c> registra los handlers escaneando dos assemblies:
	/// <c>Infrastructure</c> y <c>Core</c>. El segundo se identifica con
	/// <c>typeof(IGeneradorDocumentos)</c>, un tipo que no es un marcador pensado para eso: si
	/// ese puerto se mueve o se elimina, los handlers de <c>Core</c> dejan de registrarse sin
	/// un solo error de compilación. Esta prueba es la que lo detecta.
	/// </summary>
	[Fact]
	public void La_composicion_real_registra_los_handlers_de_eventos_definidos_en_Core()
	{
		using var provider = ComposicionRealDeInfraestructura();

		provider.GetServices<INotificationHandler<LicenciaSolicitadaEvent>>().ShouldNotBeEmpty();
		provider.GetServices<INotificationHandler<MateriaEliminadaEvent>>().ShouldNotBeEmpty();
	}

	private static ServiceProvider ComposicionRealDeInfraestructura() =>
		new ServiceCollection()
			.AddLogging()
			.AddInfrastructure()
			.BuildServiceProvider();
}
