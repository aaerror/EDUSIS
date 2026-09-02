using Domain.Cursos.DomainEvents;
using Domain.Licencias.DomainEvents;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Infraestructura;
using Infrastructure;
using Infrastructure.IntegrationTests.Infraestructura;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Infrastructure.IntegrationTests.Eventos;

/// <summary>
/// US3-3 / FR-007: <c>UnitOfWork.GuardarCambiosAsync()</c> publica los eventos de dominio
/// encolados <strong>antes</strong> de <c>SaveChangesAsync()</c>
/// (<c>MediatrExtension.DispatchDomainEventsAsync</c>), y un evento sólo se dispara si la
/// entidad pasa por el <c>ChangeTracker</c> y se guarda con ese método.
/// </summary>
public sealed class DespachoDeEventosTests : BaseIntegracion
{
	public DespachoDeEventosTests(SqlServerFixture fixture)
		: base(fixture)
	{
	}

	#region (a) GuardarCambiosAsync publica antes de SaveChanges

	[RequiereSqlServerFact]
	public async Task GuardarCambiosAsync_ejecuta_los_handlers_registrados_antes_de_persistir()
	{
		var docenteID = await SembrarDocenteAsync();
		await using var contexto = Fixture.CrearContexto();

		var licencia = new LicenciaBuilder().ConDocente(docenteID).Build();

		EntityState? estadoAlPublicar = null;
		var espia = new EspiaDeEvento<LicenciaSolicitadaEvent>(_ =>
			estadoAlPublicar = contexto.Entry(licencia).State);

		var mediator = CrearMediatorCon(espia);
		var unidad = new UnitOfWork(contexto, mediator);

		contexto.Add(licencia);
		await unidad.GuardarCambiosAsync();

		espia.Ejecutado.ShouldBeTrue();
		// El handler corrió mientras la entidad todavía estaba `Added`: aún no se hizo SaveChanges.
		estadoAlPublicar.ShouldBe(EntityState.Added);

		await using var verificacion = Fixture.CrearContexto();
		(await verificacion.Set<Domain.Licencias.Licencia>()
			.AnyAsync(x => x.Id == licencia.Id)).ShouldBeTrue();
	}

	#endregion

	#region (b) Sin GuardarCambiosAsync el evento no se publica

	[RequiereSqlServerFact]
	public async Task Un_SaveChangesAsync_directo_no_publica_los_eventos_de_dominio()
	{
		var docenteID = await SembrarDocenteAsync();
		await using var contexto = Fixture.CrearContexto();

		var licencia = new LicenciaBuilder().ConDocente(docenteID).Build();
		var espia = new EspiaDeEvento<LicenciaSolicitadaEvent>();
		_ = CrearMediatorCon(espia); // el mediator existe, pero nadie llama a DispatchDomainEventsAsync

		contexto.Add(licencia);
		await contexto.SaveChangesAsync();

		espia.Ejecutado.ShouldBeFalse();
		// La cola de eventos de la entidad sigue intacta: nunca se drenó.
		licencia.Eventos.ShouldContain(evento => evento is LicenciaSolicitadaEvent);
	}

	#endregion

	#region (c) Handlers de Core en la composición real (FR-018)

	/// <summary>
	/// Defecto H-022: <c>InfrastructureDI.AddInfrastructure</c> registra MediatR con
	/// <c>RegisterServicesFromAssemblyContaining&lt;EdusisDBContext&gt;()</c>, es decir sólo
	/// escanea el assembly de <c>Infrastructure</c>. Los <c>INotificationHandler</c> de dominio
	/// viven en <c>Core</c> (<c>Core.ServicioCursos.Events.LicenciaSolicitadaEventHandler</c>,
	/// <c>Core.ServicioCurriculas.Events.MateriaEliminadaEventHandler</c>) y <strong>no</strong>
	/// quedan registrados: los eventos se publican al vacío. Ver <c>hallazgos.md</c>.
	/// </summary>
	[Fact(Skip = "Defecto H-022: InfrastructureDI no escanea el assembly Core; sus handlers de eventos no se registran. Ver hallazgos.md")]
	public void La_composicion_real_registra_los_handlers_de_eventos_definidos_en_Core()
	{
		using var provider = ComposicionRealDeInfraestructura();

		provider.GetServices<INotificationHandler<LicenciaSolicitadaEvent>>().ShouldNotBeEmpty();
		provider.GetServices<INotificationHandler<MateriaEliminadaEvent>>().ShouldNotBeEmpty();
	}

	[RequiereSqlServerFact]
	public void Hoy_la_composicion_real_no_registra_ningun_handler_de_los_eventos_de_dominio_de_Core()
	{
		using var provider = ComposicionRealDeInfraestructura();

		provider.GetServices<INotificationHandler<LicenciaSolicitadaEvent>>().ShouldBeEmpty();
		provider.GetServices<INotificationHandler<MateriaEliminadaEvent>>().ShouldBeEmpty();
	}

	#endregion

	#region HELPERS

	private static IMediator CrearMediatorCon<TEvento>(EspiaDeEvento<TEvento> espia)
		where TEvento : INotification =>
		new ServiceCollection()
			.AddMediatR(configuracion => configuracion.RegisterServicesFromAssemblyContaining<EdusisDBContext>())
			.AddSingleton<INotificationHandler<TEvento>>(espia)
			.BuildServiceProvider()
			.GetRequiredService<IMediator>();

	private static ServiceProvider ComposicionRealDeInfraestructura() =>
		new ServiceCollection()
			.AddLogging()
			.AddInfrastructure()
			.BuildServiceProvider();

	/// <summary>Handler espía: registra que se ejecutó y ejecuta un callback opcional.</summary>
	private sealed class EspiaDeEvento<TEvento> : INotificationHandler<TEvento>
		where TEvento : INotification
	{
		private readonly Action<TEvento> _alRecibir;

		public EspiaDeEvento(Action<TEvento>? alRecibir = null) =>
			_alRecibir = alRecibir ?? (_ => { });

		public bool Ejecutado { get; private set; }

		public Task Handle(TEvento notification, CancellationToken cancellationToken)
		{
			Ejecutado = true;
			_alRecibir(notification);
			return Task.CompletedTask;
		}
	}

	#endregion
}
