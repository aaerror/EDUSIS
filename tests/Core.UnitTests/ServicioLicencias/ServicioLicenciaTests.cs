using Domain.Licencias.Exceptions;
using Core.ServicioDocentes.Exceptions;
using Core.ServicioLicencias.Exceptions;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias;
using Core.ServicioLicencias.DTOs.Requests;
using Core.Shared.Seguridad;
using Core.UnitTests.Infraestructura;
using Domain.Licencias;
using Domain.Usuarios;
using EDUSIS.TestSupport;
using EDUSIS.TestSupport.Builders;
using EDUSIS.TestSupport.Fakes;
using Shouldly;
using Xunit;

namespace Core.UnitTests.ServicioLicencias;

/// <summary>
/// <see cref="IServicioLicencia"/>: cada método público con camino feliz y de error (licencia
/// inexistente, estado incompatible, docente inexistente, superposición o permiso del artículo).
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
			docente.Id, "AccidenteDeTrabajo", DateTime.Today, Dias: 0, "Reposo"));

		_host.UnidadDeTrabajo.LicenciasFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
		_host.UnidadDeTrabajo.EventosPublicados.ShouldContain(e => e.GetType().Name == "LicenciaSolicitadaEvent");
	}

	[Fact]
	public async Task SolicitarLicencia_con_un_docente_inexistente_lanza_DocenteNoEncontrado()
	{
		await Should.ThrowAsync<DocenteNoEncontradoException>(() => _servicio.SolicitarLicencia(
			new NuevaSolicitudLicenciaRequest(Guid.NewGuid(), "AccidenteDeTrabajo", DateTime.Today, Dias: 0, "Reposo")));
	}

	[Fact]
	public async Task SolicitarLicencia_con_dias_fija_la_fecha_de_fin_de_la_licencia()
	{
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);

		var fechaInicio = DateTime.Today;
		await _servicio.SolicitarLicencia(new NuevaSolicitudLicenciaRequest(
			docente.Id, "EnfermedadCortoTratamiento", fechaInicio, Dias: 5, "Reposo"));

		var licencia = _host.UnidadDeTrabajo.LicenciasFake.Elementos.ShouldHaveSingleItem();
		licencia.Periodo.FechaFin.ShouldBe(fechaInicio.AddDays(4));
	}
	#endregion

	#region Superposición
	/// <summary>Docente sembrado con una licencia del artículo y período dados, en el estado pedido.</summary>
	private Licencia SembrarLicenciaDe(Guid docenteID, DateTime inicio, DateTime? fin, bool aprobada = false, string articulo = nameof(Articulo.AccidenteDeTrabajo))
	{
		var builder = new LicenciaBuilder().ConDocente(docenteID).ConArticulo(articulo).ConFechaInicio(inicio).ConFechaFin(fin);
		var licencia = (aprobada ? builder.Aprobada() : builder).Build();
		licencia.LiberarEventos();
		_host.UnidadDeTrabajo.LicenciasFake.Sembrar(licencia);
		return licencia;
	}

	private Guid SembrarDocente()
	{
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		return docente.Id;
	}

	[Fact]
	public async Task SolicitarLicencia_superpuesta_con_otra_del_docente_lanza_LicenciaSuperpuesta_y_no_guarda()
	{
		var docente = SembrarDocente();
		SembrarLicenciaDe(docente, DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1));

		await Should.ThrowAsync<LicenciaSuperpuestaException>(() => _servicio.SolicitarLicencia(new NuevaSolicitudLicenciaRequest(
			docente, nameof(Articulo.EnfermedadCortoTratamiento), DateTime.Today.AddDays(-1), Dias: 3, null)));

		_host.UnidadDeTrabajo.LicenciasFake.Elementos.ShouldHaveSingleItem();
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task SolicitarLicencia_que_empieza_al_dia_siguiente_del_ultimo_de_otra_se_registra()
	{
		var docente = SembrarDocente();
		SembrarLicenciaDe(docente, DateTime.Today.AddDays(-10), DateTime.Today.AddDays(-1), aprobada: true);

		await _servicio.SolicitarLicencia(new NuevaSolicitudLicenciaRequest(
			docente, nameof(Articulo.EnfermedadCortoTratamiento), DateTime.Today, Dias: 3, null));

		_host.UnidadDeTrabajo.LicenciasFake.Elementos.Count.ShouldBe(2);
	}

	[Fact]
	public async Task SolicitarLicencia_sobre_el_periodo_de_una_rechazada_se_registra()
	{
		var docente = SembrarDocente();
		var rechazada = SembrarLicenciaDe(docente, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(5));
		rechazada.RechazarLicencia(null);

		await _servicio.SolicitarLicencia(new NuevaSolicitudLicenciaRequest(
			docente, nameof(Articulo.EnfermedadCortoTratamiento), DateTime.Today, Dias: 3, null));

		_host.UnidadDeTrabajo.LicenciasFake.Elementos.Count.ShouldBe(2);
	}

	[Fact]
	public async Task Las_licencias_de_otro_docente_no_cuentan_para_la_superposicion()
	{
		var docente = SembrarDocente();
		SembrarLicenciaDe(Guid.NewGuid(), DateTime.Today.AddDays(-2), null, aprobada: true);

		await _servicio.SolicitarLicencia(new NuevaSolicitudLicenciaRequest(
			docente, nameof(Articulo.EnfermedadCortoTratamiento), DateTime.Today, Dias: 3, null));

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ModificarLicencia_hacia_el_periodo_de_otra_lanza_LicenciaSuperpuesta()
	{
		var docente = Guid.NewGuid();
		SembrarLicenciaDe(docente, DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-15));
		var pendiente = SembrarLicenciaDe(docente, DateTime.Today.AddDays(-5), DateTime.Today);

		await Should.ThrowAsync<LicenciaSuperpuestaException>(() => _servicio.ModificarLicencia(new ModificarLicenciaRequest(
			pendiente.Id, docente, nameof(Articulo.EnfermedadCortoTratamiento), DateTime.Today.AddDays(-16), Dias: 3, null)));

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task ModificarLicencia_no_choca_con_su_propio_periodo_anterior()
	{
		var docente = Guid.NewGuid();
		var pendiente = SembrarLicenciaDe(docente, DateTime.Today.AddDays(-5), DateTime.Today);

		await _servicio.ModificarLicencia(new ModificarLicenciaRequest(
			pendiente.Id, docente, nameof(Articulo.EnfermedadCortoTratamiento), DateTime.Today.AddDays(-4), Dias: 3, null));

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ExtenderLicencia_sobre_el_periodo_de_la_siguiente_lanza_LicenciaSuperpuesta()
	{
		var docente = Guid.NewGuid();
		var activa = SembrarLicenciaDe(docente, DateTime.Today.AddDays(-5), DateTime.Today.AddDays(2), aprobada: true);
		SembrarLicenciaDe(docente, DateTime.Today.AddDays(3), DateTime.Today.AddDays(6));

		await Should.ThrowAsync<LicenciaSuperpuestaException>(() => _servicio.ExtenderLicencia(
			new ExtenderLicenciaRequest(activa.Id, docente, Dias: 1)));

		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task DeclararLicenciaIndefinida_con_una_licencia_posterior_lanza_LicenciaSuperpuesta()
	{
		var docente = Guid.NewGuid();
		var activa = SembrarLicenciaDe(docente, DateTime.Today.AddDays(-5), DateTime.Today.AddDays(2), aprobada: true);
		SembrarLicenciaDe(docente, DateTime.Today.AddDays(30), DateTime.Today.AddDays(31));

		await Should.ThrowAsync<LicenciaSuperpuestaException>(() => _servicio.DeclararLicenciaIndefinida(
			new DeclararLicenciaIndefinidaRequest(activa.Id, docente)));
	}
	#endregion

	#region Licencia aprobada
	[Fact]
	public async Task FinalizarLicencia_la_finaliza_guarda_y_publica_LicenciaFinalizadaEvent()
	{
		var licencia = SembrarLicenciaDe(Guid.NewGuid(), DateTime.Today.AddDays(-5), DateTime.Today.AddDays(5), aprobada: true);

		await _servicio.FinalizarLicencia(new FinalizarLicenciaRequest(licencia.Id, licencia.DocenteID));

		licencia.Estado.ShouldBe(Estado.Finalizada);
		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
		_host.UnidadDeTrabajo.EventosPublicados.ShouldContain(e => e.GetType().Name == "LicenciaFinalizadaEvent");
	}

	[Fact]
	public async Task ExtenderLicencia_prorroga_y_guarda()
	{
		var fin = DateTime.Today.AddDays(5);
		var licencia = SembrarLicenciaDe(Guid.NewGuid(), DateTime.Today.AddDays(-5), fin, aprobada: true);

		await _servicio.ExtenderLicencia(new ExtenderLicenciaRequest(licencia.Id, licencia.DocenteID, Dias: 4));

		licencia.Periodo.FechaFin.ShouldBe(fin.AddDays(4));
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task FijarFechaFinLicencia_le_pone_termino_a_una_indefinida()
	{
		var licencia = SembrarLicenciaDe(Guid.NewGuid(), DateTime.Today.AddDays(-5), null, aprobada: true);

		await _servicio.FijarFechaFinLicencia(new FijarFechaFinLicenciaRequest(licencia.Id, licencia.DocenteID, DateTime.Today.AddDays(10)));

		licencia.Periodo.FechaFin.ShouldBe(DateTime.Today.AddDays(10));
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task DeclararLicenciaIndefinida_le_quita_la_fecha_de_fin()
	{
		var licencia = SembrarLicenciaDe(Guid.NewGuid(), DateTime.Today.AddDays(-5), DateTime.Today.AddDays(5), aprobada: true);

		await _servicio.DeclararLicenciaIndefinida(new DeclararLicenciaIndefinidaRequest(licencia.Id, licencia.DocenteID));

		licencia.EsIndefinida().ShouldBeTrue();
	}

	[Fact]
	public async Task FinalizarLicencia_inexistente_lanza_LicenciaNoEncontrada()
	{
		await Should.ThrowAsync<LicenciaNoEncontradaException>(() => _servicio.FinalizarLicencia(
			new FinalizarLicenciaRequest(Guid.NewGuid(), Guid.NewGuid())));
	}

	/// <remarks>
	/// Las operaciones sobre una aprobada piden el permiso de resolución del artículo, no el de
	/// gestión de legajos: quien carga la solicitud no decide cómo sigue.
	/// </remarks>
	[Fact]
	public async Task Con_solo_GestionarDocentes_no_se_finaliza_una_licencia()
	{
		var licencia = new LicenciaBuilder().ConFechaInicio(DateTime.Today.AddDays(-2)).ConFechaFin(DateTime.Today.AddDays(3)).Aprobada().Build();
		using var host = new HostDeServicios(uow => uow.LicenciasFake.Sembrar(licencia), SesionDePrueba.Con(Permiso.GestionarDocentes));

		await Should.ThrowAsync<PermisoDenegadoException>(() => host.Resolver<IServicioLicencia>().FinalizarLicencia(
			new FinalizarLicenciaRequest(licencia.Id, licencia.DocenteID)));

		licencia.Estado.ShouldBe(Estado.Activa);
	}

	[Fact]
	public async Task Con_ResolverLicencias_no_se_extiende_una_licencia_academica()
	{
		var licencia = new LicenciaBuilder().ConArticulo(nameof(Articulo.OtrasCausas))
			.ConFechaInicio(DateTime.Today.AddDays(-2)).ConFechaFin(DateTime.Today.AddDays(3)).Aprobada().Build();
		using var host = new HostDeServicios(uow => uow.LicenciasFake.Sembrar(licencia), SesionDePrueba.Con(Permiso.ResolverLicencias));

		await Should.ThrowAsync<PermisoDenegadoException>(() => host.Resolver<IServicioLicencia>().ExtenderLicencia(
			new ExtenderLicenciaRequest(licencia.Id, licencia.DocenteID, Dias: 2)));
	}
	#endregion

	#region ListarLicenciasPendientesAsync
	[Fact]
	public async Task ListarLicenciasPendientesAsync_trae_solo_las_pendientes_con_el_nombre_del_docente()
	{
		var docente = new DocenteBuilder().Build();
		_host.UnidadDeTrabajo.DocentesFake.Sembrar(docente);
		var pendiente = SembrarLicenciaDe(docente.Id, DateTime.Today.AddDays(-2), DateTime.Today);
		SembrarLicenciaDe(docente.Id, DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-10), aprobada: true);

		var pendientes = await _servicio.ListarLicenciasPendientesAsync();

		var fila = pendientes.ShouldHaveSingleItem();
		fila.LicenciaID.ShouldBe(pendiente.Id);
		fila.Docente.ShouldNotBeNullOrWhiteSpace();
		fila.Dias.ShouldBe(3);
	}

	[Fact]
	public async Task ListarLicenciasPendientesAsync_sin_permiso_academico_omite_las_academicas()
	{
		var comun = new LicenciaBuilder().ConFechaInicio(DateTime.Today.AddDays(-2)).ConFechaFin(DateTime.Today).Build();
		var academica = new LicenciaBuilder().ConArticulo(nameof(Articulo.OtrasCausas)).ConFechaInicio(DateTime.Today.AddDays(-2)).ConFechaFin(DateTime.Today).Build();
		using var host = new HostDeServicios(uow =>
		{
			uow.LicenciasFake.Sembrar(comun);
			uow.LicenciasFake.Sembrar(academica);
		}, SesionDePrueba.Con(Permiso.ResolverLicencias));

		var pendientes = await host.Resolver<IServicioLicencia>().ListarLicenciasPendientesAsync();

		pendientes.ShouldHaveSingleItem().LicenciaID.ShouldBe(comun.Id);
	}

	[Fact]
	public async Task ListarLicenciasPendientesAsync_sin_permisos_de_resolucion_devuelve_una_bandeja_vacia()
	{
		var licencia = new LicenciaBuilder().ConFechaInicio(DateTime.Today.AddDays(-2)).ConFechaFin(DateTime.Today).Build();
		using var host = new HostDeServicios(uow => uow.LicenciasFake.Sembrar(licencia), SesionDePrueba.Con(Permiso.GestionarDocentes));

		var pendientes = await host.Resolver<IServicioLicencia>().ListarLicenciasPendientesAsync();

		pendientes.ShouldBeEmpty();
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
	public async Task AprobarLicencia_con_una_licencia_inexistente_lanza_LicenciaNoEncontrada()
	{
		await Should.ThrowAsync<LicenciaNoEncontradaException>(() => _servicio.AprobarLicencia(
			new ActualizarEstadoLicenciaRequest(Guid.NewGuid(), Guid.NewGuid(), null)));
	}

	[Fact]
	public async Task RechazarLicencia_rechaza_la_licencia_pendiente_y_guarda()
	{
		var licencia = SembrarLicenciaPendiente();

		await _servicio.RechazarLicencia(new ActualizarEstadoLicenciaRequest(licencia.Id, licencia.DocenteID, "Sin cobertura"));

		licencia.Estado.ShouldBe(Estado.Rechazada);
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task RechazarLicencia_con_una_licencia_inexistente_lanza_LicenciaNoEncontrada()
	{
		await Should.ThrowAsync<LicenciaNoEncontradaException>(() => _servicio.RechazarLicencia(
			new ActualizarEstadoLicenciaRequest(Guid.NewGuid(), Guid.NewGuid(), null)));
	}
	#endregion

	#region Quién resuelve según el artículo
	/// <summary>Host con una sola licencia pendiente del artículo dado y la sesión acotada a los permisos indicados.</summary>
	private static (HostDeServicios Host, Licencia Licencia) HostConLicencia(string articulo, params Permiso[] permisos)
	{
		var licencia = new LicenciaBuilder()
			.ConArticulo(articulo)
			.ConFechaInicio(DateTime.Today.AddDays(-2))
			.ConFechaFin(DateTime.Today.AddDays(3))
			.Build();
		var host = new HostDeServicios(uow => uow.LicenciasFake.Sembrar(licencia), SesionDePrueba.Con(permisos));

		return (host, licencia);
	}

	[Theory]
	[InlineData(nameof(Articulo.Examen))]
	[InlineData(nameof(Articulo.EstudioEncomendado))]
	[InlineData(nameof(Articulo.OtrasCausas))]
	public async Task Con_ResolverLicencias_no_se_aprueba_una_licencia_academica_y_queda_pendiente(string articulo)
	{
		var (host, licencia) = HostConLicencia(articulo, Permiso.ResolverLicencias);
		using var _ = host;

		await Should.ThrowAsync<PermisoDenegadoException>(() => host.Resolver<IServicioLicencia>().AprobarLicencia(
			new ActualizarEstadoLicenciaRequest(licencia.Id, licencia.DocenteID, null)));

		licencia.Estado.ShouldBe(Estado.Pendiente);
		host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(0);
	}

	[Fact]
	public async Task Con_ResolverLicencias_no_se_rechaza_una_licencia_academica()
	{
		var (host, licencia) = HostConLicencia(nameof(Articulo.OtrasCausas), Permiso.ResolverLicencias);
		using var _ = host;

		await Should.ThrowAsync<PermisoDenegadoException>(() => host.Resolver<IServicioLicencia>().RechazarLicencia(
			new ActualizarEstadoLicenciaRequest(licencia.Id, licencia.DocenteID, null)));

		licencia.Estado.ShouldBe(Estado.Pendiente);
	}

	[Fact]
	public async Task Con_ResolverLicenciasAcademicas_se_aprueba_una_licencia_academica()
	{
		var (host, licencia) = HostConLicencia(nameof(Articulo.OtrasCausas), Permiso.ResolverLicenciasAcademicas);
		using var _ = host;

		await host.Resolver<IServicioLicencia>().AprobarLicencia(
			new ActualizarEstadoLicenciaRequest(licencia.Id, licencia.DocenteID, null));

		licencia.Estado.ShouldBe(Estado.Activa);
	}

	[Fact]
	public async Task Con_ResolverLicencias_se_aprueba_una_licencia_no_academica()
	{
		var (host, licencia) = HostConLicencia(nameof(Articulo.AccidenteDeTrabajo), Permiso.ResolverLicencias);
		using var _ = host;

		await host.Resolver<IServicioLicencia>().AprobarLicencia(
			new ActualizarEstadoLicenciaRequest(licencia.Id, licencia.DocenteID, null));

		licencia.Estado.ShouldBe(Estado.Activa);
	}

	/// <remarks>
	/// El permiso académico no implica el general: son dos atribuciones distintas. Secretaría
	/// Académica resuelve todas porque tiene los dos, no porque uno contenga al otro.
	/// </remarks>
	[Fact]
	public async Task Con_solo_ResolverLicenciasAcademicas_no_se_aprueba_una_licencia_no_academica()
	{
		var (host, licencia) = HostConLicencia(nameof(Articulo.AccidenteDeTrabajo), Permiso.ResolverLicenciasAcademicas);
		using var _ = host;

		await Should.ThrowAsync<PermisoDenegadoException>(() => host.Resolver<IServicioLicencia>().AprobarLicencia(
			new ActualizarEstadoLicenciaRequest(licencia.Id, licencia.DocenteID, null)));
	}
	#endregion

	#region ModificarLicencia
	[Fact]
	public async Task ModificarLicencia_actualiza_periodo_y_observaciones_de_una_licencia_pendiente()
	{
		var licencia = SembrarLicenciaPendiente();

		await _servicio.ModificarLicencia(new ModificarLicenciaRequest(
			licencia.Id, licencia.DocenteID, "EnfermedadCortoTratamiento", DateTime.Today.AddDays(-1), Dias: 3, "Prórroga"));

		licencia.Observacion.ShouldBe("Prórroga");
		_host.UnidadDeTrabajo.CantidadDeGuardados.ShouldBe(1);
	}

	[Fact]
	public async Task ModificarLicencia_con_una_licencia_inexistente_lanza_LicenciaNoEncontrada()
	{
		await Should.ThrowAsync<LicenciaNoEncontradaException>(() => _servicio.ModificarLicencia(new ModificarLicenciaRequest(
			Guid.NewGuid(), Guid.NewGuid(), "EnfermedadCortoTratamiento", DateTime.Today, Dias: 3, "Prórroga")));
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

		await Should.ThrowAsync<LicenciaNoPendienteException>(() => _servicio.EliminarLicencia(
			new EliminarLicenciaRequest(licencia.Id, licencia.DocenteID)));
		_host.UnidadDeTrabajo.LicenciasFake.Elementos.ShouldHaveSingleItem();
	}
	#endregion
}
