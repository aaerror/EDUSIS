using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using System.Threading.Tasks;
using System.Windows;
using System;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Shared.Messages;

namespace WPF_Desktop.ViewModels.Docentes;

internal partial class PerfilDocenteViewModel : ObservableObject
{
	#region Servicio
	private readonly IServicioDocente _servicioDocentes;
	#endregion

	private Guid _docenteID;
	private LegajoStore _perfilBuscadoStore;

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
	#endregion


	public PerfilDocenteViewModel(IServicioDocente servicioDocentes, LegajoStore perfilBuscadoStore)
	{
		_servicioDocentes = servicioDocentes;
		_perfilBuscadoStore = perfilBuscadoStore;

		if (string.IsNullOrWhiteSpace(_perfilBuscadoStore.Documento))
		{
			throw new ArgumentNullException(nameof(perfilBuscadoStore.Documento), "Error al ver el perfil del docente. Primero debe buscar un docente antes de continuar.");
		}

		CargarPerfilCommandAsync = new AsyncRelayCommand(CargarPerfilCommandAsyncExecute);
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
		var request = new DocenteIDRequest(_perfilBuscadoStore.PersonaID);
		_docenteTemporal = await _servicioDocentes.VerPerfilDocenteAsync(request);
		Docente = _docenteTemporal;
	}

	public async Task CargarPerfilCommandAsyncExecute()
	{
		var request = new DocenteIDRequest(_perfilBuscadoStore.PersonaID);
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
				MessageBox.Show("Error al habilitar la edición.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
		CargarPerfilCommandAsyncExecute();

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

	private async Task GuardarCommandExecute(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		switch (obj)
		{
			case "Contacto":
				messageBoxText = $"Se van a registrar los siguientes datos de cambio de contacto:\n" +
								 $"Email: { Docente.ContactoDTO.Email }\n" +
								 $"Teléfono: { Docente.ContactoDTO.Telefono}\n\n" +
								 $"¿Desea continuar?";
				caption = "Actualización de Contacto";
				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						_cambiarContactoRequest = new CambiarContactoRequest(Docente.DocenteID,
																			 Docente.ContactoDTO.Telefono,
																			 Docente.ContactoDTO.Email);
						await _servicioDocentes.ActualizarContacto(_cambiarContactoRequest);

						MessageBox.Show("¡Datos actualizados correctamente!", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
						HabilitarEditarContacto = false;
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				break;

			case "Domicilio":
				messageBoxText = $"Se van a registrar los siguientes datos de cambio de domicilio:\n" +
								 $"Calle con Altura: { Docente.DomicilioDTO.Calle } { Docente.DomicilioDTO.Altura } ({ Docente.DomicilioDTO.Vivienda })\n" +
								 $"Observaciones: {Docente.DomicilioDTO.Observacion }\n" +
								 $"Localidad: { Docente.DomicilioDTO.Localidad }, {Docente.DomicilioDTO.Provincia }, { Docente.DomicilioDTO.Pais }";
				caption = "Actualización de Domicilio";
				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result is MessageBoxResult.Yes)
				{
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

						MessageBox.Show("¡Datos actualizados correctamente!", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
						HabilitarEditarDomicilio = false;
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				break;

			case "Sexo":
				messageBoxText = $"Se van a registrar los siguientes datos de cambio de sexo del docente:\n" +
								 $"Nombre Completo: { Docente.InformacionPersonalDTO.Apellido }, {Docente.InformacionPersonalDTO.Nombre }\n" +
								 $"Sexo: { Docente.InformacionPersonalDTO.Sexo }\n\n" +
								 $"¿Desea continuar?";
				caption = "Cambio de Sexo";
				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						_cambiarSexoRequest = new CambiarSexoRequest(Docente.DocenteID,
																	 Docente.InformacionPersonalDTO.Apellido,
																	 Docente.InformacionPersonalDTO.Nombre,
																	 Docente.InformacionPersonalDTO.Sexo);
						await _servicioDocentes.ActualizarSexo(_cambiarSexoRequest);

						MessageBox.Show("¡Datos actualizados correctamente!", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
						HabilitarEditarSexo = false;
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
}