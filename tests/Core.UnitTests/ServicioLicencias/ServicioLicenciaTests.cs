using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias;
using Core.ServicioLicencias.DTOs.Requests;
using Core.UnitTests.Infraestructura;
using Domain.Licencias;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioLicencias;

/// <summary>
/// <see cref="IServicioLicencia"/>: los 6 métodos públicos con camino feliz y de error
/// (licencia inexistente, estado incompatible o docente inexistente).
/// </summary>
[Trait("Categoria", Categorias.Unidad)]
public class ServicioLicenciaTests
{
	private readonly HostDeServicios _host = new();
	private readonly IServicioLicencia _servicio;

	public ServicioLicenciaTests()
	{
		_servicio = _host.Resolver<IServicioLicencia>();
	}

	private Licencia SembrarLicenciaPendiente(Guid? docenteID = null)
	{
		var licencia = new LicenciaBuilder()
			.ConDocente(docenteID ?? Guid.NewGuid())
			.ConFechaInicio(DateTime.Today.AddDays(-5))
			.ConFechaFin(DateTime.Today.AddDays(5))
			.Build();
		_host.UnidadDeTrabajo.LicenciasFake.Sembrar(licencia);
		return licencia;
	}

	#region BuscarLicenciasSegunDocenteAsync
	[Fact]
	public async Task BuscarLicenciasSegunDocenteAsync_devuelve_las_licencias_del_docente()
	{
		var docenteID = Guid.NewGuid();
		SembrarLicenciaPendiente(docenteID);
		SembrarLicenciaPendiente(docenteID);
		SembrarLicenciaPendiente(Guid.NewGuid());

		var licencias = await _servicio.BuscarLicenciasSegunDocenteAsync(new DocenteIDRequest(docenteID));

		licencias.Count.ShouldBe(2);
		licencias.ShouldAllBe(x => x.DocenteID == docenteID);
	}

	[Fact]
	public async Task BuscarLicenciasSegunDocenteAsync_devuelve_coleccion_vacia_si_el_docente_no_tiene_licencias()
	{
		var licencias = await _servicio.BuscarLicenciasSegunDocenteAsync(new DocenteIDRequest(Guid.NewGuid()));

		licencias.ShouldBeEmpty();
	}
	#endregion

	#region SolicitarLicencia
	[Fact]
	public async Task SolicitarLicencia_da_de_alta_la_licencia_de_un_docente_existente_y_publica_el_evento()
	{
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		await _servicio.SolicitarLicencia(new NuevaSolicitudLicenciaRequest(
			docente.Id, "Enfermedad", DateTime.Today, Dias: 0, "Reposo"));

		_host.UnidadDeTrabajo.LicenciasFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
		_host.UnidadDeTrabajo.EventosPublicados.ShouldContain(e => e.GetType().Name == "LicenciaSolicitadaEvent");
	}

	[Fact]
	public async Task SolicitarLicencia_con_un_docente_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.SolicitarLicencia(
			new NuevaSolicitudLicenciaRequest(Guid.NewGuid(), "Enfermedad", DateTime.Today, Dias: 0, "Reposo")));
	}

	[Fact(Skip = "H-016: SolicitarLicencia con Dias > 0 llama EstablecerFechaFinalizacion sobre una licencia recién creada (estado Pendiente); el guard !EstaActiva() lanza LicenciaInactivaException y la solicitud con plazo nunca se completa. Ver hallazgos.md.")]
	public void SolicitarLicencia_con_dias_fija_la_fecha_de_fin_de_la_licencia()
	{
	}
	#endregion

	#region AprobarLicencia / RechazarLicencia
	[Fact]
	public async Task AprobarLicencia_pasa_la_licencia_pendiente_a_activa_y_guarda()
	{
		var licencia = SembrarLicenciaPendiente();

		await _servicio.AprobarLicencia(new ActualizarEstadoLicenciaRequest(licencia.Id, licencia.DocenteID, "Aprobada por dirección"));

		licencia.Estado.ShouldBe(Estado.Activa);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task AprobarLicencia_con_una_licencia_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.AprobarLicencia(
			new ActualizarEstadoLicenciaRequest(Guid.NewGuid(), Guid.NewGuid(), null)));
	}

	[Fact]
	public async Task RechazarLicencia_cancela_la_licencia_pendiente_y_guarda()
	{
		var licencia = SembrarLicenciaPendiente();

		await _servicio.RechazarLicencia(new ActualizarEstadoLicenciaRequest(licencia.Id, licencia.DocenteID, "Sin cobertura"));

		licencia.Estado.ShouldBe(Estado.Cancelada);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RechazarLicencia_con_una_licencia_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.RechazarLicencia(
			new ActualizarEstadoLicenciaRequest(Guid.NewGuid(), Guid.NewGuid(), null)));
	}
	#endregion

	#region ModificarLicencia
	[Fact]
	public async Task ModificarLicencia_actualiza_periodo_y_observaciones_de_una_licencia_pendiente()
	{
		var licencia = SembrarLicenciaPendiente();

		await _servicio.ModificarLicencia(new ModificarLicenciaRequest(
			licencia.Id, licencia.DocenteID, "Enfermedad", DateTime.Today.AddDays(-1), Dias: 3, "Prórroga"));

		licencia.Observacion.ShouldBe("Prórroga");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ModificarLicencia_con_una_licencia_inexistente_lanza_NullReferenceException()
	{
		await Should.ThrowAsync<NullReferenceException>(() => _servicio.ModificarLicencia(new ModificarLicenciaRequest(
			Guid.NewGuid(), Guid.NewGuid(), "Enfermedad", DateTime.Today, Dias: 3, "Prórroga")));
	}
	#endregion

	#region EliminarLicencia
	[Fact]
	public async Task EliminarLicencia_borra_una_licencia_pendiente_y_guarda()
	{
		var licencia = SembrarLicenciaPendiente();

		await _servicio.EliminarLicencia(new EliminarLicenciaRequest(licencia.Id, licencia.DocenteID));

		_host.UnidadDeTrabajo.LicenciasFake.Elementos.ShouldBeEmpty();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task EliminarLicencia_rechaza_borrar_una_licencia_que_no_esta_pendiente()
	{
		var licencia = new LicenciaBuilder()
			.ConFechaInicio(DateTime.Today.AddDays(-5))
			.ConFechaFin(DateTime.Today.AddDays(5))
			.Aprobada()
			.Build();
		_host.UnidadDeTrabajo.LicenciasFake.Sembrar(licencia);

		await Should.ThrowAsync<ArgumentException>(() => _servicio.EliminarLicencia(
			new EliminarLicenciaRequest(licencia.Id, licencia.DocenteID)));
		_host.UnidadDeTrabajo.LicenciasFake.Elementos.ShouldHaveSingleItem();
	}
	#endregion
}
