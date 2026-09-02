using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos;
using Core.Shared.DTOs.Personas.Response;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared.Commands;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Alumnos;

internal partial class GestionAlumnosViewModel : ObservableValidator
{
	#region Servicio
	private readonly IServicioAlumno _servicioAlumnos;
	private readonly INavigationService _registrarAlumnoNavigationService;
	private readonly INavigationService _perfilNavigationService;
	private readonly INavigationService _inscripcionAlumnoNavigationService;
	#endregion

	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private LegajoStore _perfilStore;

	[Required(AllowEmptyStrings=true, ErrorMessage="Se debe ingresar el documento del alumno.")]
	[RegularExpression(@"^\d{8}$", ErrorMessage="El documento debe ser un número.", MatchTimeoutInMilliseconds=2000)]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(BuscarCommand))]
	[ObservableProperty]
	private string _documento;

	[NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private PersonaResponse _personaResponse;

	[ObservableProperty]
	private string _message = string.Empty;

	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private bool _existeAlumno;

	#region COMMANDs
	public IRelayCommand EliminarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	public IRelayCommand BuscarCommand { get; }

	public ViewModelCommand QuitarCommand { get;  }
	#endregion


	public GestionAlumnosViewModel(IServicioAlumno servicioAlumnos,
								   INavigationService registrarAlumnoNavigationService,
								   INavigationService inscripcionAlumnoNavigationService,
								   INavigationService verPerfilNavigationService,
								   LegajoStore perfilBuscadoStore)
	{
		_servicioAlumnos = servicioAlumnos;
		_registrarAlumnoNavigationService = registrarAlumnoNavigationService;
		_inscripcionAlumnoNavigationService = inscripcionAlumnoNavigationService;
		_perfilNavigationService = verPerfilNavigationService;

		_perfilStore = perfilBuscadoStore;

		BuscarCommand = new RelayCommand(ExecuteBuscarCommand, CanExecuteBuscarCommand);
		EliminarCommand = new RelayCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);

		Message = "Debe buscar un alumno para proceder.";
		ExisteAlumno = false;
	}

	#region BuscarCommand
	private bool CanExecuteBuscarCommand() =>
		!string.IsNullOrWhiteSpace(Documento) && !HasErrors;

	private async void ExecuteBuscarCommand()
	{
		try
		{
			var response = await _servicioAlumnos.BuscarPorDNIAsync(new DocumentoRequest(Documento));
			if (response is null)
			{
				ExisteAlumno = false;
				Message = $"No se encontró el alumno con el D.N.I. { Documento }. Por favor, intente nuevamente.";

				return;
			}

			PersonaResponse = response;
			_perfilStore.PersonaID = PersonaResponse.PersonaID;
			_perfilStore.Documento = PersonaResponse.Documento;

			ExisteAlumno = true;
			Message = string.Empty;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
	#endregion

	#region NavigationCommand
	private bool CanExecuteNavigationCommand(object obj) => obj switch
	{
		"Asignar" => PersonaResponse is not null,
		"Perfil" => PersonaResponse is not null,
		"Registrar" => true,
		_ => false
	};

	private void ExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Asignar":
				_inscripcionAlumnoNavigationService.Navigate();
				break;

			case "Perfil":
				_perfilNavigationService.Navigate();
				break;

			case "Registrar":
				_registrarAlumnoNavigationService.Navigate();
				break;
		}
	}
	#endregion

	#region EliminarCommand
	private bool CanExecuteEliminarCommand() =>
		PersonaResponse is not null;

	private async void ExecuteEliminarCommand()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		messageBoxText = $"¿Está seguro que desea quitar el alumno { PersonaResponse.Apellido }, { PersonaResponse.Nombre }?";
		caption = "Eliminar Alumno de la Institución";

		result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (result is MessageBoxResult.Yes)
		{
			try
			{
				var request = new EliminarAlumnoRequest(PersonaResponse.PersonaID);
				await _servicioAlumnos.EliminarAlumnoAsync(request);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
	#endregion
}
