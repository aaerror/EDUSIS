using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Shared.Messages;
using Core.Shared.DTOs.Personas.Requests;

namespace WPF_Desktop.ViewModels.Docentes;

internal partial class GestionDocentesViewModel : ObservableValidator
{
	#region Servicios
	private readonly IServicioDocente _servicioDocentes;
	#endregion

	#region NavigationService
	private readonly INavigationService _registrarDocenteNavigationService;
	private readonly INavigationService _perfilDocenteNavigationService;
	private readonly INavigationService _gestionPuestosNavigationService;
	private readonly INavigationService _gestionLicenciasNavigationService;
	#endregion

	private LegajoStore _perfilBuscadoStore;

	[Required(AllowEmptyStrings=false, ErrorMessage="Ingresar el nombre del docente.")]
	[NotifyCanExecuteChangedFor(nameof(BuscarCommandAsync))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _searchedText;

	[ObservableProperty]
	private string _notificacion;

	[ObservableProperty]
	private bool _habilitarNotificacion;

	[ObservableProperty]
	private string _documentoNacionalIdentidad;

	[ObservableProperty]
	private bool _mostrarVista;

	[ObservableProperty]
	private bool _habilitarListaDocentes;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	private LegajoDocenteViewModel _legajoDocenteSeleccionado;

	[ObservableProperty]
	private ObservableCollection<LegajoDocenteViewModel> _legajosDocentes = new();

	#region Commands
	public IRelayCommand RegistrarCommand { get; }
	public IAsyncRelayCommand BuscarCommandAsync { get; }
	public IRelayCommand EliminarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionDocentesViewModel(IServicioDocente servicioDocentes,
									INavigationService registrarDocenteNavigationService,
									INavigationService perfilDocenteNavigationService,
									INavigationService gestionPuestoNavigationService,
									INavigationService gestionLicenciasNavigationService,
									LegajoStore perfilBuscadoStore)
	{
		_servicioDocentes = servicioDocentes;
		_registrarDocenteNavigationService = registrarDocenteNavigationService;
		_perfilDocenteNavigationService = perfilDocenteNavigationService;
		_gestionPuestosNavigationService = gestionPuestoNavigationService;
		_gestionLicenciasNavigationService = gestionLicenciasNavigationService;
		_perfilBuscadoStore = perfilBuscadoStore;

		RegistrarCommand = new RelayCommand<string>(commnad =>
		{
			_registrarDocenteNavigationService.Navigate();
		});
		BuscarCommandAsync = new AsyncRelayCommand(ExecuteBuscarCommandAsync, CanExecuteBuscarCommand);
		EliminarCommand = new RelayCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);

		MostrarVista = false;
		HabilitarNotificacion = false;
		HabilitarListaDocentes = false;
	}

	#region BuscarCommand
	private bool CanExecuteBuscarCommand() =>
		!HasErrors;

	private async Task ExecuteBuscarCommandAsync()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		HabilitarNotificacion = false;
		Notificacion = string.Empty;
		MostrarVista = true;

		try
		{
			var response = await _servicioDocentes.BuscarDocenteSegunNombreCompletoAsync(new NombreCompletoRequest(SearchedText));

			if (response.Count is 0)
			{
				/**
				 * messageBoxText = $"No se ha encontrado ningún docente. Vuelva a intentarlo nuevamente.";
				 * caption = "Resultado de la búsqueda";
				 * MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
				 */

				Notificacion = "No se ha encontrado docentes en la búsqueda.\nVuelva a intentarlo nuevamente.";
				HabilitarNotificacion = true;
				HabilitarListaDocentes = false;

				return;
			}

			LegajosDocentes.Clear();
			foreach (var legajoDocente in response)
			{
				LegajosDocentes.Add(new LegajoDocenteViewModel(legajoDocente));
			}

			HabilitarListaDocentes = true;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
	#endregion

	#region EliminarCommand
	private bool CanExecuteEliminarCommand()
	{
		bool canExecute = false;
		if (LegajoDocenteSeleccionado is not null)
		{
			canExecute = true;
		}

		return canExecute;
	}

	private void ExecuteEliminarCommand()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		if (LegajoDocenteSeleccionado is null)
		{
			messageBoxText = "Se debe buscar previamente el docente para poder realizar los cambios que necesite.";
			caption = "Quitar Docente";
			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

			return;
		}

		messageBoxText = $"¿Está seguro que desea quitar el docente {LegajoDocenteSeleccionado.NombreCompleto}?";
		caption = "Quitar Docente";
		result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (result is MessageBoxResult.Yes)
		{
			try
			{
				_servicioDocentes.QuitarDocente(new DocenteIDRequest(LegajoDocenteSeleccionado.DocenteID));
				messageBoxText = $"El docente, {LegajoDocenteSeleccionado.NombreCompleto}, se quitó correctamente.";
				caption = "Operación Exitosa";

				MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
				LegajoDocenteSeleccionado = new LegajoDocenteViewModel(null);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
	#endregion

	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj)
	{
		bool canExecute = false;
		if (LegajoDocenteSeleccionado is not null)
		{
			canExecute = true;
		}

		return canExecute;
	}

	private void ExecuteNavigationCommand(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;

		if (LegajoDocenteSeleccionado is null)
		{
			messageBoxText = "Se debe buscar previamente el docente que desea visitar el perfil.";
			caption = "Perfil Docente";
			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

			return;
		}

		_perfilBuscadoStore.PersonaID = LegajoDocenteSeleccionado.DocenteID;
		_perfilBuscadoStore.Documento = LegajoDocenteSeleccionado.DocumentoNacionalIdentidad;

		switch (obj)
		{
			case "Licencia":
				_gestionLicenciasNavigationService.Navigate();
				break;
			case "Perfil":
				WeakReferenceMessenger.Default.Send(new DocenteSeleccionadoMessage(_perfilBuscadoStore.PersonaID));
				_perfilDocenteNavigationService.Navigate();
				break;
			case "Puesto":
				_gestionPuestosNavigationService.Navigate();
				break;
		}
	}
	#endregion
}