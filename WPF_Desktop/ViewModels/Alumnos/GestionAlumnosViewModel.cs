using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos;
using Core.Shared.DTOs.Personas.Response;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Alumnos;

internal partial class GestionAlumnosViewModel : ObservableValidator
{
	#region Servicio
	private readonly IServicioAlumno _servicioAlumnos;
	private readonly IDialogService _dialogService;
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
	public IAsyncRelayCommand EliminarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	public IAsyncRelayCommand BuscarCommand { get; }
	#endregion


	public GestionAlumnosViewModel(IServicioAlumno servicioAlumnos,
								   IDialogService dialogService,
								   INavigationService registrarAlumnoNavigationService,
								   INavigationService inscripcionAlumnoNavigationService,
								   INavigationService verPerfilNavigationService,
								   LegajoStore perfilBuscadoStore)
	{
		_servicioAlumnos = servicioAlumnos;
		_dialogService = dialogService;
		_registrarAlumnoNavigationService = registrarAlumnoNavigationService;
		_inscripcionAlumnoNavigationService = inscripcionAlumnoNavigationService;
		_perfilNavigationService = verPerfilNavigationService;

		_perfilStore = perfilBuscadoStore;

		BuscarCommand = new AsyncRelayCommand(ExecuteBuscarCommand, CanExecuteBuscarCommand);
		EliminarCommand = new AsyncRelayCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);

		Message = "Buscá un alumno por su documento para ver su perfil, inscribirlo o quitarlo. Si todavía no está cargado, usá «Registrar alumno».";
		ExisteAlumno = false;
	}

	#region BuscarCommand
	private bool CanExecuteBuscarCommand() =>
		!string.IsNullOrWhiteSpace(Documento) && !HasErrors;

	/// <summary>
	/// Busca el alumno por documento exacto.
	/// </summary>
	/// <remarks>
	/// Era <c>async void</c> sobre un <c>RelayCommand</c>: cualquier excepción que no cayera en
	/// el try —por ejemplo al escribir en el store— no la podía atrapar nadie y terminaba en el
	/// manejador global, que apaga la aplicación. Como <c>AsyncRelayCommand</c> además expone
	/// <c>IsRunning</c>, la pantalla puede mostrar que está buscando.
	/// </remarks>
	private async Task ExecuteBuscarCommand()
	{
		try
		{
			var response = await _servicioAlumnos.BuscarPorDNIAsync(new DocumentoRequest(Documento));
			if (response is null)
			{
				ExisteAlumno = false;
				Message = $"No hay ningún alumno con el documento { Documento }. Revisá el número o registralo con «Registrar alumno».";

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
			_dialogService.MostrarError(ex.Message, "No se pudo buscar el alumno");
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

	/// <summary>
	/// Quita el alumno de la institución.
	/// </summary>
	/// <remarks>
	/// Tenía tres problemas. Era <c>async void</c>; la confirmación no decía qué se pierde; y
	/// al terminar NO limpiaba la pantalla: la ficha del alumno recién borrado seguía ahí, con
	/// los botones "Perfil" e "Inscribir" habilitados sobre un registro que ya no existe.
	/// </remarks>
	private async Task ExecuteEliminarCommand()
	{
		string nombre = $"{ PersonaResponse.Apellido }, { PersonaResponse.Nombre }";

		// El verbo del diálogo es el del botón que lo abrió: la pantalla dice "Eliminar" y el
		// caso de uso es EliminarAlumno, así que acá no aparece un "Quitar" que no se usó.
		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Eliminar a { nombre } de la institución",
				consecuencia: $"El legajo del documento { PersonaResponse.Documento } deja de estar disponible, "
							+ "junto con su domicilio y su contacto. Las inscripciones y calificaciones ya registradas "
							+ "quedan en el historial de la división. Esta acción no se puede deshacer.",
				verbo: "Eliminar alumno")))
		{
			return;
		}

		try
		{
			var request = new EliminarAlumnoRequest(PersonaResponse.PersonaID);
			await _servicioAlumnos.EliminarAlumnoAsync(request);

			// La pantalla vuelve a su estado inicial: dejar la ficha de un alumno borrado es
			// la forma más rápida de que alguien opere sobre un registro que ya no existe.
			PersonaResponse = null;
			ExisteAlumno = false;
			Documento = string.Empty;
			Message = $"{ nombre } ya no figura en la institución. Buscá otro documento para seguir.";

			_dialogService.MostrarInformacion($"{ nombre } ya no figura en la institución.", "Alumno eliminado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo quitar el alumno");
		}
	}
	#endregion
}
