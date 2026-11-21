using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioLicencias;
using Core.ServicioLicencias.DTOs.Requests;
using Core.Shared.Seguridad;
using Domain.Usuarios;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Licencias;

/// <summary>
/// Bandeja de licencias pendientes de todos los docentes, para quien las resuelve: Dirección y
/// Secretaría Académica deciden desde acá sin entrar al legajo de cada docente.
/// </summary>
/// <remarks>
/// <para>
/// Qué filas ve la sesión lo decide <c>Core</c> (<c>ListarLicenciasPendientesAsync</c> omite las
/// que no puede resolver); acá sólo se habilitan los botones con el mismo permiso por artículo que
/// exige el servicio. La seguridad sigue en <c>Core</c>.
/// </para>
/// <para>
/// La observación se edita en la bandeja y no en la fila: es lo que se guarda con la decisión
/// (el motivo del rechazo, quién autoriza). Arranca con la de la solicitud porque el servicio la
/// reemplaza, y mandarla vacía borraría lo que se cargó con la solicitud.
/// </para>
/// </remarks>
internal partial class LicenciasPendientesViewModel : ObservableObject
{
	private readonly IServicioLicencia _servicioLicencia;
	private readonly IDialogService _dialogService;
	private readonly ISesionActual _sesion;

	[ObservableProperty]
	private ObservableCollection<LicenciaPendienteViewModel> _pendientes = new();

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(AprobarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RechazarCommandAsync))]
	[NotifyPropertyChangedFor(nameof(HaySeleccion))]
	private LicenciaPendienteViewModel? _seleccionada;

	[ObservableProperty]
	private string? _observacion;

	/// <summary>No hay pendientes que la sesión pueda resolver: la bandeja muestra el aviso en lugar del listado.</summary>
	[ObservableProperty]
	private bool _bandejaVacia;

	public bool HaySeleccion => Seleccionada is not null;

	#region Commands
	public IAsyncRelayCommand CargarCommandAsync { get; }
	public IAsyncRelayCommand AprobarCommandAsync { get; }
	public IAsyncRelayCommand RechazarCommandAsync { get; }
	#endregion


	public LicenciasPendientesViewModel(IServicioLicencia servicioLicencia,
										IDialogService dialogService,
										ISesionActual sesion)
	{
		_servicioLicencia = servicioLicencia;
		_dialogService = dialogService;
		_sesion = sesion;

		CargarCommandAsync = new AsyncRelayCommand(ExecuteCargarCommandAsync);
		AprobarCommandAsync = new AsyncRelayCommand(ExecuteAprobarCommandAsync, PuedeResolver);
		RechazarCommandAsync = new AsyncRelayCommand(ExecuteRechazarCommandAsync, PuedeResolver);
	}

	/// <summary>
	/// El permiso cambia con el artículo de la fila elegida, así que no se puede envolver el
	/// CanExecute en un <c>Permite</c> de permiso fijo: es el mismo criterio que
	/// <c>ServicioLicencia.PermisoParaResolver</c>.
	/// </summary>
	private bool PuedeResolver() =>
		Seleccionada is not null
		&& _sesion.TienePermiso(Seleccionada.ResolucionAcademica ? Permiso.ResolverLicenciasAcademicas : Permiso.ResolverLicencias);

	partial void OnSeleccionadaChanged(LicenciaPendienteViewModel? value) =>
		Observacion = value?.Observacion;

	public async Task ExecuteCargarCommandAsync()
	{
		try
		{
			var pendientes = await _servicioLicencia.ListarLicenciasPendientesAsync();

			Pendientes = new ObservableCollection<LicenciaPendienteViewModel>(pendientes.Select(x => new LicenciaPendienteViewModel(x)));
			Seleccionada = null;
			BandejaVacia = Pendientes.Count == 0;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudieron cargar las licencias pendientes");
		}
	}

	private async Task ExecuteAprobarCommandAsync()
	{
		var licencia = Seleccionada!;

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
				titulo: $"Aprobar la licencia de { licencia.Docente }",
				mensaje: $"{ licencia.Denominacion }: { DescribirPeriodo(licencia) }. "
					   + "La licencia deja de estar pendiente y el docente deja de estar en funciones en sus cátedras.",
				verbo: "Aprobar licencia")))
		{
			return;
		}

		await DecidirAsync(licencia,
			() => _servicioLicencia.AprobarLicencia(new ActualizarEstadoLicenciaRequest(licencia.LicenciaID, licencia.DocenteID, Observacion)),
			"Licencia aprobada", "No se pudo aprobar la licencia");
	}

	private async Task ExecuteRechazarCommandAsync()
	{
		var licencia = Seleccionada!;

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
				titulo: $"Rechazar la licencia de { licencia.Docente }",
				mensaje: $"{ licencia.Denominacion }: { DescribirPeriodo(licencia) }. "
					   + "La solicitud queda rechazada y no se puede aprobar después. La observación queda como motivo.",
				verbo: "Rechazar licencia")))
		{
			return;
		}

		await DecidirAsync(licencia,
			() => _servicioLicencia.RechazarLicencia(new ActualizarEstadoLicenciaRequest(licencia.LicenciaID, licencia.DocenteID, Observacion)),
			"Licencia rechazada", "No se pudo rechazar la licencia");
	}

	private async Task DecidirAsync(LicenciaPendienteViewModel licencia, Func<Task> decision, string tituloExito, string tituloError)
	{
		try
		{
			await decision();

			_dialogService.MostrarInformacion($"{ licencia.Docente }: { licencia.Denominacion }.", tituloExito);
			await ExecuteCargarCommandAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, tituloError);
		}
	}

	private static string DescribirPeriodo(LicenciaPendienteViewModel licencia) =>
		licencia.FechaFin.HasValue
			? $"{ licencia.Dias } días, del { licencia.FechaInicio:dd/MM/yyyy} al { licencia.FechaFin:dd/MM/yyyy}"
			: $"indefinida desde el { licencia.FechaInicio:dd/MM/yyyy}";
}
