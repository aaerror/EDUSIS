using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Docentes;

internal partial class PerfilDocenteViewModel : ObservableObject, IUbicacion
{
	#region Servicio
	private readonly IServicioDocente _servicioDocentes;
	private readonly IDialogService _dialogService;
	#endregion

	private LegajoStore _perfilBuscadoStore;

	/// <summary>
	/// Ruta hasta acá. Esta pantalla era una de las siete sin ninguna salida: se entraba desde
	/// el índice de docentes y el único camino de vuelta era elegir otra sección en el rail.
	/// </summary>
	public IReadOnlyList<Migaja> Ruta { get; }

	#region Response
	private PerfilDocenteResponse _docenteTemporal;

	[ObservableProperty]
	private PerfilDocenteResponse _docente;
	#endregion

	#region Request
	private CambiarSexoRequest _cambiarSexoRequest = null;
	private CambiarContactoRequest _cambiarContactoRequest = null;
	private CambiarDomicilioRequest _cambiarDomicilioRequest = null;
	#endregion

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	private bool _habilitarEditarSexo;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	private bool _habilitarEditarContacto;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(EditarCommand))]
	[NotifyCanExecuteChangedFor(nameof(CancelarCommand))]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	private bool _habilitarEditarDomicilio;

	#region Commands
	public IAsyncRelayCommand CargarPerfilCommandAsync { get; }
	public IAsyncRelayCommand CancelarCommand { get; }
	public IRelayCommand EditarCommand { get; }
	public IAsyncRelayCommand GuardarCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion


	public PerfilDocenteViewModel(IServicioDocente servicioDocentes,
								  IDialogService dialogService,
								  INavigationService gestionDocentesNavigationService,
								  LegajoStore perfilBuscadoStore)
	{
		_servicioDocentes = servicioDocentes;
		_dialogService = dialogService;
		_perfilBuscadoStore = perfilBuscadoStore;

		// UI-34: la guarda cubre las dos propiedades del store porque se escriben juntas. Con
		// PersonaID tipado como Guid no nulable, "nadie buscó un docente" llegaba como Guid.Empty y
		// el request salía igual hacia Core, que lo rechazaba sin que la pantalla explicara nada.
		if (_perfilBuscadoStore.PersonaID is null || string.IsNullOrWhiteSpace(_perfilBuscadoStore.Documento))
		{
			throw new ArgumentNullException(nameof(perfilBuscadoStore), "Error al ver el perfil del docente. Primero debe buscar un docente antes de continuar.");
		}

		CargarPerfilCommandAsync = new AsyncRelayCommand(CargarPerfilCommandAsyncExecute);
		VolverCommand = new RelayCommand(gestionDocentesNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("DOCENTES", VolverCommand),
			Migaja.Actual("PERFIL")
		};

		EditarCommand = new RelayCommand<string>(EditarCommandExecute, EditarCommandCanExecute);
		CancelarCommand = new AsyncRelayCommand<string>(CancelarCommandExecute, CancelarCommandCanExecute);
		GuardarCommand = new AsyncRelayCommand<string>(GuardarCommandExecute, GuardarCommandCanExecute);

		HabilitarEditarSexo = false;
		HabilitarEditarDomicilio = false;
		HabilitarEditarContacto = false;
	}

	public async Task CargarPerfilDocenteAsync()
	{
		//var request = new DocenteIDRequest(DocenteID: _docenteID);
		// .Value es seguro: el constructor ya rechaza un store sin persona elegida.
		var request = new DocenteIDRequest(_perfilBuscadoStore.PersonaID.Value);
		_docenteTemporal = await _servicioDocentes.VerPerfilDocenteAsync(request);
		Docente = _docenteTemporal;
	}

	public async Task CargarPerfilCommandAsyncExecute()
	{
		// .Value es seguro: el constructor ya rechaza un store sin persona elegida.
		var request = new DocenteIDRequest(_perfilBuscadoStore.PersonaID.Value);
		_docenteTemporal = await _servicioDocentes.VerPerfilDocenteAsync(request);
		Docente = _docenteTemporal;
	}

	#region EditarCommand
	private bool EditarCommandCanExecute(object obj)
	{
		switch (obj as string)
		{
			case "Contacto":
				return !HabilitarEditarContacto;
			case "Domicilio":
				return !HabilitarEditarDomicilio;
			case "Sexo":
				return !HabilitarEditarSexo;
			default:
				return false;
		}
	}

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

	#region CancelarCommand
	private bool CancelarCommandCanExecute(object obj)
	{
		switch (obj as string)
		{
			case "Contacto":
				return HabilitarEditarContacto;
			case "Domicilio":
				return HabilitarEditarDomicilio;
			case "Sexo":
				return HabilitarEditarSexo;
			default:
				return false;
		}
	}

	private async Task CancelarCommandExecute(object obj)
	{
		await CargarPerfilCommandAsyncExecute();

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
			default: return;
		}
	}
	#endregion

	#region GuardarCommand
	private bool GuardarCommandCanExecute(object obj)
	{
		switch (obj as string)
		{
			case "Contacto":
				return HabilitarEditarContacto;
			case "Domicilio":
				return HabilitarEditarDomicilio;
			case "Sexo":
				return HabilitarEditarSexo;
			default:
				return false;
		}
	}

	/// <summary>
	/// Guarda el bloque que se estaba editando.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Ya no pregunta "¿Desea continuar?". Cada bloque se abre con "Editar", se modifica a la
	/// vista y se cierra con "Guardar cambios" o con "Cancelar": pedir confirmación encima de
	/// eso era un cuarto paso que repetía en un cartel los datos que el usuario tiene delante.
	/// La confirmación se reserva para lo que no se puede deshacer; esto se vuelve a editar.
	/// </para>
	/// <para>
	/// Lo que sí faltaba es la respuesta: antes el éxito salía con "¡Datos actualizados
	/// correctamente!" en un MessageBox y había que despacharlo. Ahora el aviso al pie nombra
	/// el bloque, así que se sabe cuál de los tres se guardó.
	/// </para>
	/// </remarks>
	private async Task GuardarCommandExecute(object obj)
	{
		switch (obj)
		{
			case "Contacto":
				try
				{
					_cambiarContactoRequest = new CambiarContactoRequest(Docente.DocenteID,
																		 Docente.ContactoDTO.Telefono,
																		 Docente.ContactoDTO.Email);
					await _servicioDocentes.ActualizarContacto(_cambiarContactoRequest);

					HabilitarEditarContacto = false;
					_dialogService.MostrarInformacion($"Teléfono { Docente.ContactoDTO.Telefono } y correo { Docente.ContactoDTO.Email }.", "Contacto guardado");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo guardar el contacto");
				}
				break;

			case "Domicilio":
				try
				{
					_cambiarDomicilioRequest = new CambiarDomicilioRequest(Docente.DocenteID,
																		   Docente.DomicilioDTO.Calle,
																		   Docente.DomicilioDTO.Altura,
																		   Docente.DomicilioDTO.Vivienda,
																		   Docente.DomicilioDTO.Observacion,
																		   Docente.DomicilioDTO.Localidad,
																		   Docente.DomicilioDTO.Provincia,
																		   Docente.DomicilioDTO.Pais);
					await _servicioDocentes.ActualizarDomicilio(_cambiarDomicilioRequest);

					HabilitarEditarDomicilio = false;
					_dialogService.MostrarInformacion($"{ Docente.DomicilioDTO.Calle } { Docente.DomicilioDTO.Altura }, { Docente.DomicilioDTO.Localidad }.", "Domicilio guardado");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo guardar el domicilio");
				}
				break;

			case "Sexo":
				try
				{
					_cambiarSexoRequest = new CambiarSexoRequest(Docente.DocenteID,
																 Docente.InformacionPersonalDTO.Apellido,
																 Docente.InformacionPersonalDTO.Nombre,
																 Docente.InformacionPersonalDTO.Sexo);
					await _servicioDocentes.ActualizarSexo(_cambiarSexoRequest);

					HabilitarEditarSexo = false;
					_dialogService.MostrarInformacion($"{ Docente.InformacionPersonalDTO.Apellido }, { Docente.InformacionPersonalDTO.Nombre }.", "Datos personales guardados");
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