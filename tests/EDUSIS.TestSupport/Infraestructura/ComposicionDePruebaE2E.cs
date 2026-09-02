using Core.Shared;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EDUSIS.TestSupport.Infraestructura;

/// <summary>
/// Arma el <see cref="ServiceProvider"/> real de las pruebas end-to-end
/// (contracts/test-support-api.md §5.3, FR-008, FR-014, FR-017): la misma composición que en
/// producción —<c>InfrastructureDI.AddInfrastructure()</c> → <c>CoreDI.AddServices()</c>— con
/// la <strong>única</strong> sustitución de la cadena del <see cref="EdusisDBContext"/> por la
/// de la base efímera de prueba.
///
/// <para>
/// No se toca <c>InfrastructureDI</c> ni <c>CoreDI</c>: el resto del grafo (<c>IUnitOfWork</c>,
/// MediatR, repositorios, handlers) queda tal cual está en producción. La redirección se hace
/// quitando del <see cref="IServiceCollection"/> los descriptores de <see cref="EdusisDBContext"/>
/// y sus <c>DbContextOptions</c> y volviéndolos a registrar contra la cadena de prueba
/// (patrón <c>WebApplicationFactory</c>).
/// </para>
/// </summary>
public static class ComposicionDePruebaE2E
{
	/// <summary>
	/// Construye la composición real apuntando <see cref="EdusisDBContext"/> a
	/// <paramref name="cadenaDeConexion"/> (la de <see cref="SqlServerFixture.CadenaDeConexion"/>,
	/// nunca la hardcodeada de <c>InfrastructureDI</c> — FR-016). El llamador es dueño del
	/// <see cref="ServiceProvider"/> devuelto y debe liberarlo.
	/// </summary>
	public static ServiceProvider Construir(string cadenaDeConexion)
	{
		var servicios = new ServiceCollection();

		servicios.AddLogging();
		servicios.AddInfrastructure();
		servicios.AddServices();

		RedirigirElDbContext(servicios, cadenaDeConexion);

		return servicios.BuildServiceProvider();
	}

	/// <summary>
	/// Quita el <see cref="EdusisDBContext"/> y sus <c>DbContextOptions</c> registrados por
	/// <c>InfrastructureDI</c> (contra <c>localhost</c> / <c>EdusisDB</c>) y los vuelve a
	/// registrar contra la base de prueba, con el mismo <see cref="ServiceLifetime.Scoped"/>.
	/// </summary>
	private static void RedirigirElDbContext(IServiceCollection servicios, string cadenaDeConexion)
	{
		servicios.RemoveAll<DbContextOptions<EdusisDBContext>>();
		servicios.RemoveAll<DbContextOptions>();
		servicios.RemoveAll<EdusisDBContext>();

		servicios.AddDbContext<EdusisDBContext>(
			opciones => opciones.UseSqlServer(cadenaDeConexion),
			ServiceLifetime.Scoped);
	}
}
