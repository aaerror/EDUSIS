using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Requests;
using Core.ServicioLicencias;
using Domain.Licencias;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using Microsoft.IdentityModel.Tokens;

namespace WPF_Desktop.ViewModels.Docentes.Licencias;

internal partial class GestionLicenciasViewModel : ObservableValidator, IUbicacion
{
	#region Servicios
	private readonly IServicioLicencia _servicioLicencia;
	private readonly IDialogService _dialogService;
	#endregion

	/// <summary>
	/// Docente cuyas licencias se gestionan, fijado una sola vez al construir.
	/// </summary>
	/// <remarks>
	/// UI-34: <c>LegajoStore.PersonaID</c> pasó a ser <c>Guid?</c>, y la guarda va acá una vez en
	/// lugar de repetir un <c>.Value</c> en los seis sitios que arman requests. Llegar a esta
	/// pantalla sin docente elegido es un defecto de navegación y no un estado de la pantalla:
	/// <c>GestionDocentesViewModel</c> escribe el store antes de navegar.
	/// </remarks>
	private readonly Guid _docenteID;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	private bool _habilitarEdicion;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	private bool _habilitarDetalleLicencias;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	private bool _habilitarUpsert;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	private bool _habilitarRegistro;

	[ObservableProperty]
	private bool _habilitarMessage;

	[ObservableProperty]
	private string _message = string.Empty;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
	private LicenciaViewModel _licenciaSeleccionada;

	[ObservableProperty]
	private ObservableCollection<LicenciaViewModel> _licencias = new();

	#region Commands
	public IAsyncRelayCommand CargarLicenciasCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommandAsync { get; }
	public IAsyncRelayCommand GuardarCommandAsync { get; }
	public IRelayCommand EditarCommand { get; }
	public IRelayCommand RegistrarCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion

	public IReadOnlyList<Migaja> Ruta { get; }


	public GestionLicenciasViewModel(IServicioLicencia servicioLicencia,
									 IDialogService dialogService,
									 INavigationService gestionDocentesNavigationService,
									 LegajoStore perfilBuscadoStore)
	{
		_servicioLicencia = servicioLicencia;
		_dialogService = dialogService;
		_docenteID = perfilBuscadoStore.PersonaID
			?? throw new ArgumentNullException(nameof(perfilBuscadoStore), "Error al gestionar las licencias. Primero debe buscar un docente antes de continuar.");

		VolverCommand = new RelayCommand(gestionDocentesNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("DOCENTES", VolverCommand),
			Migaja.Actual("LICENCIAS")
		};

		CargarLicenciasCommandAsync = new AsyncRelayCommand(ExecuteCargarLicenciasCommand);
		CancelarCommandAsync = new AsyncRelayCommand<string>(ExecuteCancelarCommand, CanExecuteCancelarCommand);
		GuardarCommandAsync = new AsyncRelayCommand<string>(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);
		EditarCommand = new RelayCommand<string>(ExecuteEditarCommand, CanExecuteEditarCommand);
		RegistrarCommand = new RelayCommand<string>(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);

		HabilitarUpsert = false;
		HabilitarRegistro = false;
		HabilitarEdicion = false;
	}

	public async Task ExecuteCargarLicenciasCommand()
	{
		try
		{
			var request = new DocenteIDRequest(_docenteID);
			var licencias = await _servicioLicencia.BuscarLicenciasSegunDocenteAsync(request);

			if (licencias.IsNullOrEmpty())
			{
				Message = "Este docente no tiene licencias registradas. Cargá la primera con «Registrar licencia».";
				HabilitarMessage = true;
				HabilitarDetalleLicencias = false;
			}
			else
			{
				Licencias = new ObservableCollection<LicenciaViewModel>(licencias.Select(x => new LicenciaViewModel(x)));

				Message = string.Empty;
				HabilitarMessage = false;
				HabilitarDetalleLicencias = true;
			}
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudieron cargar las licencias");
		}
	}

	#region CancelarCommandAsync
	private bool CanExecuteCancelarCommand(object obj)
	{
		switch (obj)
		{
			case "Editar":
				return HabilitarUpsert && HabilitarEdicion && LicenciaSeleccionada is not null;

			case "Nueva":
				return HabilitarUpsert && HabilitarRegistro;

			default:
				return false;
		}
	}

	private async Task ExecuteCancelarCommand(object obj)
	{
		await ExecuteCargarLicenciasCommand();

		switch (obj)
		{
			case "Editar":
				HabilitarUpsert = false;
				HabilitarEdicion = false;

				break;

			case "Nueva":
				HabilitarUpsert = false;
				HabilitarRegistro = false;

				break;

			default:
				return;
		}
	}
	#endregion

	#region GuardarCommandAsync
	private bool CanExecuteGuardarCommand(object obj)
	{
		switch (obj)
		{
			case "Aprobar":
				return HabilitarDetalleLicencias && !HabilitarEdicion && LicenciaSeleccionada is not null && LicenciaSeleccionada.Estado.Equals(Enum.GetName(Estado.Pendiente));
			case "Cancelar":
				return HabilitarDetalleLicencias && !HabilitarEdicion && LicenciaSeleccionada is not null && LicenciaSeleccionada.Estado.Equals(Enum.GetName(Estado.Pendiente));
			case "Editar":
				return HabilitarUpsert && HabilitarEdicion && LicenciaSeleccionada is not null && !LicenciaSeleccionada.HasErrors;
			case "Nueva":
				return HabilitarUpsert && HabilitarRegistro && LicenciaSeleccionada is not null && !LicenciaSeleccionada.HasErrors;
			case "Eliminar":
				return HabilitarDetalleLicencias && !HabilitarEdicion && LicenciaSeleccionada is not null && LicenciaSeleccionada.Estado.Equals(Enum.GetName(Estado.Pendiente));
			default:
				return false;
		}
	}

	/// <summary>
	/// Las cinco escrituras de la pantalla: aprobar, rechazar, editar, registrar y eliminar.
	/// </summary>
	/// <remarks>
	/// <para>
	/// QUÉ PREGUNTA Y QUÉ NO. Antes las cinco abrían el mismo cartel —"¿Está seguro que
	/// desea…?", con ícono de información— así que la única irreversible no se distinguía de un
	/// alta. Ahora:
	/// </para>
	/// <list type="bullet">
	/// <item><b>Aprobar</b> y <b>rechazar</b> preguntan, sin rojo: sacan la licencia de
	/// Pendiente y después los dos botones se apagan, así que es una decisión de una sola vez.
	/// El diálogo dice qué pasa con los días.</item>
	/// <item><b>Editar</b> y <b>registrar</b> no preguntan: el usuario completó el formulario y
	/// apretó "Guardar"; la licencia queda pendiente y se puede volver a editar.</item>
	/// <item><b>Eliminar</b> pregunta en rojo y con la consecuencia: no se puede deshacer.</item>
	/// </list>
	/// </remarks>
	private async Task ExecuteGuardarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Aprobar":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Aprobar la licencia { LicenciaSeleccionada.Articulo }",
						mensaje: $"Quedan concedidos { LicenciaSeleccionada.Dias } días desde el { LicenciaSeleccionada.FechaInicio:dd/MM/yyyy}. "
							   + "La licencia deja de estar pendiente y ya no se puede editar ni rechazar.",
						verbo: "Aprobar licencia")))
				{
					return;
				}

				try
				{
					var request = new ActualizarEstadoLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
																	  DocenteID: _docenteID,
																	  Observacion: LicenciaSeleccionada.Observacion);
					await _servicioLicencia.AprobarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Articulo }, { LicenciaSeleccionada.Dias } días desde el { LicenciaSeleccionada.FechaInicio:dd/MM/yyyy}.", "Licencia aprobada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo aprobar la licencia");
				}
				break;

			case "Cancelar":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
						titulo: $"Rechazar la licencia { LicenciaSeleccionada.Articulo }",
						mensaje: "La solicitud queda rechazada y deja de estar pendiente: no se puede editar ni aprobar después. "
							   + "La observación que escribas queda como motivo.",
						verbo: "Rechazar licencia")))
				{
					return;
				}

				try
				{
					var request = new ActualizarEstadoLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
																	  DocenteID: _docenteID,
																	  Observacion: LicenciaSeleccionada.Observacion);
					await _servicioLicencia.RechazarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Articulo }.", "Licencia rechazada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo rechazar la licencia");
				}
				break;

			case "Editar":
				try
				{
					var request = new ModificarLicenciaRequest(DocenteID: _docenteID,
															   LicenciaID: LicenciaSeleccionada.LicenciaID,
															   Articulo: LicenciaSeleccionada.Articulo,
															   Dias: LicenciaSeleccionada.Dias,
															   FechaInicio: LicenciaSeleccionada.FechaInicio,
															   Observacion: LicenciaSeleccionada.Observacion);
					await _servicioLicencia.ModificarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Articulo }, { LicenciaSeleccionada.Dias } días.", "Licencia actualizada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo actualizar la licencia");
				}

				HabilitarUpsert = false;
				HabilitarEdicion = false;

				break;

			case "Nueva":
				try
				{
					var request = new NuevaSolicitudLicenciaRequest(DocenteID: _docenteID,
																	Articulo: LicenciaSeleccionada.Articulo,
																	FechaInicio: LicenciaSeleccionada.FechaInicio,
																	Dias: LicenciaSeleccionada.Dias,
																	Observacion: LicenciaSeleccionada.Observacion);
					await _servicioLicencia.SolicitarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Articulo }, { LicenciaSeleccionada.Dias } días desde el { LicenciaSeleccionada.FechaInicio:dd/MM/yyyy}. Queda pendiente de aprobación.", "Licencia registrada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo registrar la licencia");
				}

				HabilitarUpsert = false;
				HabilitarRegistro = false;

				break;

			case "Eliminar":
				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Eliminar la licencia { LicenciaSeleccionada.Articulo }",
						consecuencia: $"Se borran los { LicenciaSeleccionada.Dias } días solicitados desde el { LicenciaSeleccionada.FechaInicio:dd/MM/yyyy} "
									+ "y la licencia desaparece del legajo. Esta acción no se puede deshacer.",
						verbo: "Eliminar licencia")))
				{
					return;
				}

				try
				{
					var request = new EliminarLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
															  DocenteID: _docenteID);
					await _servicioLicencia.EliminarLicencia(request);

					_dialogService.MostrarInformacion($"{ LicenciaSeleccionada.Articulo }.", "Licencia eliminada");
					await ExecuteCargarLicenciasCommand();
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo eliminar la licencia");
				}

				break;
		}
	}
	#endregion

	#region EditarCommand
	private bool CanExecuteEditarCommand(object obj)
	{
		switch (obj)
		{
			case "Editar":
				return HabilitarDetalleLicencias && LicenciaSeleccionada is not null && LicenciaSeleccionada.Estado.Equals(Enum.GetName(Estado.Pendiente));
			default:
				return false;
		}
	}

	private void ExecuteEditarCommand(object obj)
	{
		switch (obj)
		{
			case "Editar":
				HabilitarDetalleLicencias = false;
				HabilitarUpsert = true;
				HabilitarEdicion = true;

				break;
		}
	}
	#endregion

	#region RegistrarCommand
	private bool CanExecuteRegistrarCommand(object obj)
	{
		switch (obj)
		{
			case "Nueva":
				return !HabilitarRegistro;
			default:
				return false;
		}
	}

	private void ExecuteRegistrarCommand(object obj)
	{
		switch (obj)
		{
			case "Nueva":
				HabilitarMessage = false;
				HabilitarDetalleLicencias = false;

				HabilitarUpsert = true;
				HabilitarRegistro = true;

				LicenciaSeleccionada = new LicenciaViewModel();

				break;
		}
	}
	#endregion
}