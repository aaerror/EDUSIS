using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioLicencias.DTOs.Requests;
using Core.ServicioLicencias;
using Domain.Licencias;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System;
using WPF_Desktop.Store;
using Microsoft.IdentityModel.Tokens;

namespace WPF_Desktop.ViewModels.Docentes.Licencias;

internal partial class GestionLicenciasViewModel : ObservableValidator
{
	#region Servicios
	private readonly IServicioLicencia _servicioLicencia;
	#endregion

	private LegajoStore _perfilBuscadoStore;

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
	#endregion


	public GestionLicenciasViewModel(IServicioLicencia servicioLicencia, LegajoStore perfilBuscadoStore)
	{
		_servicioLicencia = servicioLicencia;
		_perfilBuscadoStore = perfilBuscadoStore;

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
		var request = new DocenteIDRequest(_perfilBuscadoStore.PersonaID);
		var licencias = await _servicioLicencia.BuscarLicenciasSegunDocenteAsync(request);

		if (licencias.IsNullOrEmpty())
		{
			Message = "No existen licencias cargadas en el docente.";
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

	private async Task ExecuteGuardarCommandAsync(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		switch (obj)
		{
			case "Aprobar":
				messageBoxText = $"¿Está seguro que desea aprobar la licencia { LicenciaSeleccionada.Articulo } para el docente?";
				caption = "Conceder Licencia";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new ActualizarEstadoLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
																		  DocenteID: _perfilBuscadoStore.PersonaID,
																		  Observacion: LicenciaSeleccionada.Observacion);
						await _servicioLicencia.AprobarLicencia(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						await ExecuteCargarLicenciasCommand();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				break;

			case "Cancelar":
				messageBoxText = $"¿Está seguro que desea rechazar la licencia { LicenciaSeleccionada.Articulo } para el docente?";
				caption = "Rechazar Licencia";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new ActualizarEstadoLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
																		  DocenteID: _perfilBuscadoStore.PersonaID,
																		  Observacion: LicenciaSeleccionada.Observacion);
						await _servicioLicencia.RechazarLicencia(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						await ExecuteCargarLicenciasCommand();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				break;

			case "Editar":
				messageBoxText = $"¿Está seguro que desea modificar la licencia del docente?";
				caption = "Editar Licencia";
				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new ModificarLicenciaRequest(DocenteID: _perfilBuscadoStore.PersonaID,
																   LicenciaID: LicenciaSeleccionada.LicenciaID,
																   Articulo: LicenciaSeleccionada.Articulo,
																   Dias: LicenciaSeleccionada.Dias,
																   FechaInicio: LicenciaSeleccionada.FechaInicio,
																   Observacion: LicenciaSeleccionada.Observacion);
						await _servicioLicencia.ModificarLicencia(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						await ExecuteCargarLicenciasCommand();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				HabilitarUpsert = false;
				HabilitarEdicion = false;

				break;

			case "Nueva":
				messageBoxText = $"¿Está seguro que desea cargar una licencia de { LicenciaSeleccionada.Articulo } con una duración de { LicenciaSeleccionada.Dias } días?";
				caption = "Registrar Licencia";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new NuevaSolicitudLicenciaRequest(DocenteID: _perfilBuscadoStore.PersonaID,
																		Articulo: LicenciaSeleccionada.Articulo,
																		FechaInicio: LicenciaSeleccionada.FechaInicio,
																		Dias: LicenciaSeleccionada.Dias,
																		Observacion: LicenciaSeleccionada.Observacion);
						await _servicioLicencia.SolicitarLicencia(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						await ExecuteCargarLicenciasCommand();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}

				HabilitarUpsert = false;
				HabilitarRegistro = false;

				break;

			case "Eliminar":
				messageBoxText = $"¿Está seguro que desea eliminar la { LicenciaSeleccionada.Articulo }?";
				caption = "Eliminar Licencia";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new EliminarLicenciaRequest(LicenciaID: LicenciaSeleccionada.LicenciaID,
																  DocenteID: _perfilBuscadoStore.PersonaID);
						await _servicioLicencia.EliminarLicencia(request);

						messageBoxText = $"Cambios guardados exitósamente.";
						caption = "Operación Exitosa";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

						await ExecuteCargarLicenciasCommand();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
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