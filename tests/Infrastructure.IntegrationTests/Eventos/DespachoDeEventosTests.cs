using Domain.Docentes;
using Domain.Docentes.DomainEvents;
using Domain.Licencias.DomainEvents;
using Domain.Materias.DomainEvents;
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
	/// La composición real de infraestructura registra los handlers de eventos de Core.
	/// </summary>
	[Fact]
	public void La_composicion_real_registra_los_handlers_de_eventos_definidos_en_Core()
	{
		using var provider = ComposicionRealDeInfraestructura();

		provider.GetServices<INotificationHandler<LicenciaSolicitadaEvent>>().ShouldNotBeEmpty();
		provider.GetServices<INotificationHandler<MateriaEliminadaEvent>>().ShouldNotBeEmpty();
	}

	#endregion

	#region (d) Despacho en cascada de eventos

	/// <summary>
	/// Un handler puede encolar eventos nuevos en otra entidad trackeada mientras se despacha:
	/// el handler de <see cref="LicenciaSolicitadaEvent"/> desafecta al docente, lo que encola
	/// <see cref="DocenteDesafectadoDomainEvent"/>, y ese segundo evento también se publica en
	/// el mismo <c>GuardarCambiosAsync</c>, una sola vez.
	/// </summary>
	[RequiereSqlServerFact]
	public async Task GuardarCambiosAsync_publica_los_eventos_que_los_handlers_encolan_en_cascada()
	{
		var docenteID = await SembrarDocenteAsync();
		await using var contexto = Fixture.CrearContexto();

		var docente = await contexto.Set<Docente>().SingleAsync(x => x.Id == docenteID);
		var licencia = new LicenciaBuilder().ConDocente(docenteID).Build();

		var espiaDeLicencia = new EspiaDeEvento<LicenciaSolicitadaEvent>(_ => docente.Desafectar());
		var espiaDeDocente = new EspiaDeEvento<DocenteDesafectadoDomainEvent>();

		var mediator = CrearMediator(servicios => servicios
			.AddSingleton<INotificationHandler<LicenciaSolicitadaEvent>>(espiaDeLicencia)
			.AddSingleton<INotificationHandler<DocenteDesafectadoDomainEvent>>(espiaDeDocente));
		var unidad = new UnitOfWork(contexto, mediator);

		contexto.Add(licencia);
		await unidad.GuardarCambiosAsync();

		espiaDeLicencia.Recibidos.ShouldBe(1);
		espiaDeDocente.Recibidos.ShouldBe(1);
		docente.Eventos.ShouldBeEmpty();
	}

	#endregion

	#region HELPERS

	private static IMediator CrearMediator(Action<IServiceCollection> registrarHandlers)
	{
		var servicios = new ServiceCollection()
			.AddMediatR(configuracion => configuracion.RegisterServicesFromAssemblyContaining<EdusisDBContext>());
		registrarHandlers(servicios);

		return servicios
			.BuildServiceProvider()
			.GetRequiredService<IMediator>();
	}

	private static IMediator CrearMediatorCon<TEvento>(EspiaDeEvento<TEvento> espia)
		where TEvento : INotification =>
		CrearMediator(servicios => servicios.AddSingleton<INotificationHandler<TEvento>>(espia));

	private static ServiceProvider ComposicionRealDeInfraestructura() =>
		new ServiceCollection()
			.AddLogging()
			.AddInfrastructure()
			.BuildServiceProvider();

	/// <summary>Handler espía: cuenta cuántas veces se ejecutó y ejecuta un callback opcional.</summary>
	private sealed class EspiaDeEvento<TEvento> : INotificationHandler<TEvento>
		where TEvento : INotification
	{
		private readonly Action<TEvento> _alRecibir;

		public EspiaDeEvento(Action<TEvento>? alRecibir = null) =>
			_alRecibir = alRecibir ?? (_ => { });

		public int Recibidos { get; private set; }

		public bool Ejecutado => Recibidos > 0;

		public Task Handle(TEvento notification, CancellationToken cancellationToken)
		{
			Recibidos++;
			_alRecibir(notification);
			return Task.CompletedTask;
		}
	}

	#endregion
}
