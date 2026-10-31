using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Alumnos;

internal partial class PerfilAlumnoViewModel : ObservableValidator, IUbicacion
{
	#region Servicio
	private readonly IServicioAlumno _servicioAlumno;
	private readonly IDialogService _dialogService;
	#endregion

	private LegajoStore _perfilBuscadoStore;

	[ObservableProperty]
	private PersonaConDetallesResponse _alumno;

	#region Request
	private ContactoRequest _datosContactoTemporal;
	private CambiarSexoRequest _datosSexoTemporal;
	#endregion


	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	[ObservableProperty]
	private bool _habilitarEditarContacto = false;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	[ObservableProperty]
	private bool _habilitarEditarDomicilio = false;

	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	[ObservableProperty]
	private bool _habilitarEditarSexo = false;

	#region Commands
	public IAsyncRelayCommand CargarPerfilCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommand { get; }
	public IRelayCommand EditarCommand { get; }
	public IAsyncRelayCommand GuardarCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion

	/// <summary>Ruta hasta acá. La pantalla no tenía ninguna salida hacia Alumnos.</summary>
	public IReadOnlyList<Migaja> Ruta { get; }


	public PerfilAlumnoViewModel(IServicioAlumno servicioAlumno,
								 IDialogService dialogService,
								 INavigationService gestionAlumnosNavigationService,
								 LegajoStore perfilBuscadoStore)
	{
		_servicioAlumno = servicioAlumno;
		_dialogService = dialogService;
		_perfilBuscadoStore = perfilBuscadoStore;

		// UI-34: la guarda cubre las dos propiedades del store porque se escriben juntas. Con
		// PersonaID tipado como Guid no nulable, "nadie buscó un alumno" llegaba como Guid.Empty y
		// el request salía igual hacia Core, que lo rechazaba sin que la pantalla explicara nada.
		if (_perfilBuscadoStore.PersonaID is null || string.IsNullOrWhiteSpace(_perfilBuscadoStore.Documento))
		{
			throw new ArgumentNullException(nameof(perfilBuscadoStore), "Error al ver el perfil del alumno. Primero debe buscar un alumno antes de continuar.");
		}

		// _persona = new NotifyTaskCompletion<PersonaConDetallesResponse>(_servicioAlumno.BuscarPorIDAsync(perfilBuscadoStore.Documento));
		// _alumno = _persona.Result;

		CargarPerfilCommandAsync = new AsyncRelayCommand(CargarPerfilAsync);
		VolverCommand = new RelayCommand(gestionAlumnosNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("ALUMNOS", VolverCommand),
			Migaja.Actual("PERFIL")
		};

		EditarCommand = new RelayCommand<string>(EditarCommandExecute, EditarCommandCanExecute);
		CancelarCommand = new AsyncRelayCommand<string>(CancelarCommandExecute, CancelarCommandCanExecute);
		GuardarCommand = new AsyncRelayCommand<string>(GuardarCommandExecute, GuardarCommandCanExecute);
	}

	/// <summary>
	/// Trae el perfil del alumno elegido.
	/// </summary>
	/// <remarks>
	/// Era <c>public async void CargarPerfil()</c> y la vista lo llamaba con un
	/// <c>CallMethodAction</c>. Dos problemas: una excepción adentro de un <c>async void</c> no
	/// la puede atrapar nadie —se va al manejador global, que apaga la aplicación— y un método
	/// suelto no tiene <c>IsRunning</c>, así que la pantalla no podía mostrar que estaba
	/// cargando. Ahora es el mismo comando que usa el perfil del docente, que es la misma
	/// pantalla sobre otra entidad.
	/// </remarks>
	private async Task CargarPerfilAsync()
	{
		try
		{
			// .Value es seguro: el constructor ya rechaza un store sin persona elegida.
			var request = new PersonaRequest(_perfilBuscadoStore.PersonaID.Value);
			Alumno = await _servicioAlumno.BuscarPorIDAsync(request);
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo cargar el perfil del alumno");
		}
	}

	#region Properties
	public PersonaConDetallesResponse PersonaConDetalles
	{
		get
		{
			return _alumno;
		}

		set
		{
			_alumno = value;
			OnPropertyChanged(nameof(PersonaConDetalles));
		}
	}
	#endregion

	#region CancelarCommand
	private bool CancelarCommandCanExecute(object obj) => obj switch
	{
		"Contacto" => HabilitarEditarContacto,
		"Domicilio" => HabilitarEditarDomicilio,
		"Sexo" => HabilitarEditarSexo,
		_ => false
	};

	private async Task CancelarCommandExecute(object obj)
	{
		await CargarPerfilAsync();

		switch (obj as string)
		{
			case "Contacto":
				HabilitarEditarContacto = false;
				break;

			case "Domicilio":
				HabilitarEditarDomicilio = false;
				break;

			case "Sexo":
				HabilitarEditarSexo = false;
				break;

			default:
				return;
		}
	}
	#endregion

	#region EditarCommand
	private bool EditarCommandCanExecute(object obj) => obj switch
	{
		"Contacto" => !HabilitarEditarContacto,
		"Domicilio" => !HabilitarEditarDomicilio,
		"Sexo" => !HabilitarEditarSexo,
		_ => false
	};

	private void EditarCommandExecute(object obj)
	{
		switch (obj as string)
		{
			case "Contacto":
				HabilitarEditarContacto = true;
				break;
			case "Domicilio":
				HabilitarEditarDomicilio = true;
				break;
			case "Sexo":
				HabilitarEditarSexo = true;
				break;
			default:
				_dialogService.MostrarError("El bloque pedido no existe en esta pantalla.", "No se pudo abrir la edición");
				break;
		}
	}
	#endregion

	#region GuardarCommand
	private bool GuardarCommandCanExecute(object obj) => obj switch
	{
		"Contacto" => HabilitarEditarContacto,
		"Domicilio" => HabilitarEditarDomicilio,
		"Sexo" => HabilitarEditarSexo,
		_ => false
	};

	/// <summary>
	/// Guarda el bloque que se estaba editando. Mismo criterio que el perfil del docente: el
	/// usuario abrió el bloque, lo modificó a la vista y apretó "Guardar cambios"; un cartel
	/// que le repita lo que tiene delante y le pregunte "¿Desea continuar?" es un paso de más.
	/// </summary>
	private async Task GuardarCommandExecute(object obj)
	{
		switch (obj)
		{
			case "Contacto":
				try
				{
					var request = new CambiarContactoRequest(Alumno.PersonaID,
															 PersonaConDetalles.Telefono,
															 PersonaConDetalles.Email);
					await _servicioAlumno.ActualizarContacto(request);

					HabilitarEditarContacto = false;
					await CargarPerfilAsync();
					_dialogService.MostrarInformacion($"Teléfono { PersonaConDetalles.Telefono } y correo { PersonaConDetalles.Email }.", "Contacto guardado");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo guardar el contacto");
				}
				break;

			case "Domicilio":
				try
				{
					var request = new CambiarDomicilioRequest(Alumno.PersonaID,
															  PersonaConDetalles.Calle,
															  PersonaConDetalles.Altura,
															  PersonaConDetalles.Vivienda,
															  PersonaConDetalles.Observacion,
															  PersonaConDetalles.Localidad,
															  PersonaConDetalles.Provincia,
															  PersonaConDetalles.Pais);
					await _servicioAlumno.ActualizarDomicilio(request);

					HabilitarEditarDomicilio = false;
					await CargarPerfilAsync();
					_dialogService.MostrarInformacion($"{ PersonaConDetalles.Calle } { PersonaConDetalles.Altura }, { PersonaConDetalles.Localidad }.", "Domicilio guardado");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo guardar el domicilio");
				}
				break;

			case "Sexo":
				try
				{
					var request = new CambiarSexoRequest(Alumno.PersonaID,
														 PersonaConDetalles.Apellido,
														 PersonaConDetalles.Nombre,
														 PersonaConDetalles.Sexo);

					await _servicioAlumno.ActualizarSexo(request);

					HabilitarEditarSexo = false;
					await CargarPerfilAsync();
					_dialogService.MostrarInformacion($"{ PersonaConDetalles.Apellido }, { PersonaConDetalles.Nombre }.", "Datos personales guardados");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo guardar los datos personales");
				}
				break;
		}
	}
	#endregion
}