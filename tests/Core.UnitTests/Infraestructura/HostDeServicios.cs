using Core.ServicioSecurity;
using Core.Shared;
using EDUSIS.TestSupport.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Core.UnitTests.Infraestructura;

/// <summary>
/// Monta el grafo real de servicios de <c>Core</c> (<see cref="CoreDI.AddServices"/>) sobre un
/// <see cref="UnitOfWorkFake"/>, con <c>AddLogging()</c> y sin nada de <c>Infrastructure</c>: ni
/// base de datos, ni <c>InfrastructureDI</c>, ni MediatR real. Cada instancia es un contexto
/// aislado; se descarta con <c>using</c> al terminar la prueba.
/// </summary>
/// <remarks>
/// La "vía directa" para <see cref="IServicioSeguridad"/> (interfaz <c>internal</c>, visible por
/// <c>InternalsVisibleTo</c>) es <see cref="Seguridad"/>: <see cref="CoreDI"/> ya registra
/// <c>ServicioSeguridad</c> contra esa interfaz, así que se resuelve del mismo contenedor.
/// </remarks>
internal sealed class HostDeServicios : IDisposable
{
	private readonly ServiceProvider _provider;

	public UnitOfWorkFake UnidadDeTrabajo { get; }

	public HostDeServicios(Action<UnitOfWorkFake>? sembrar = null)
	{
		UnidadDeTrabajo = new UnitOfWorkFake();
		sembrar?.Invoke(UnidadDeTrabajo);

		var servicios = new ServiceCollection();
		servicios.AddLogging();
		servicios.AddSingleton<Domain.Shared.IUnitOfWork>(UnidadDeTrabajo);
		servicios.AddServices();

		_provider = servicios.BuildServiceProvider();
	}

	/// <summary>Resuelve un servicio del grafo real de <c>Core</c> (p. ej. <c>IServicioAlumno</c>).</summary>
	public T Resolver<T>()
		where T : notnull =>
		_provider.GetRequiredService<T>();

	/// <summary>Acceso directo a la implementación real de <see cref="IServicioSeguridad"/>.</summary>
	public IServicioSeguridad Seguridad => _provider.GetRequiredService<IServicioSeguridad>();

	public void Dispose() => _provider.Dispose();
}
