using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Response;
using System.Windows;
using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Alumnos;

internal partial class PerfilAlumnoViewModel : ObservableValidator
{
	#region Servicio
	private readonly IServicioAlumno _servicioAlumno;
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
	public IRelayCommand CancelarCommand { get; }
	public IRelayCommand EditarCommand { get; }
	public IRelayCommand GuardarCommand { get; }
	#endregion


	public PerfilAlumnoViewModel(IServicioAlumno servicioAlumno, LegajoStore perfilBuscadoStore)
	{
		_servicioAlumno = servicioAlumno;
		_perfilBuscadoStore = perfilBuscadoStore;

		if (string.IsNullOrWhiteSpace(_perfilBuscadoStore.Documento))
		{
			throw new ArgumentNullException(nameof(perfilBuscadoStore.Documento), "Error al ver el perfil del alumno. Primero debe buscar un alumno antes de continuar.");
		}

		// _persona = new NotifyTaskCompletion<PersonaConDetallesResponse>(_servicioAlumno.BuscarPorIDAsync(perfilBuscadoStore.Documento));
		// _alumno = _persona.Result;

		EditarCommand = new RelayCommand<string>(EditarCommandExecute, EditarCommandCanExecute);
		CancelarCommand = new RelayCommand<string>(CancelarCommandExecute, CancelarCommandCanExecute);
		GuardarCommand = new RelayCommand<string>(GuardarCommandExecute, GuardarCommandCanExecute);
	}

	public async void CargarPerfil()
	{
		var request = new PersonaRequest(_perfilBuscadoStore.PersonaID);
		var alumno = await _servicioAlumno.BuscarPorIDAsync(request);
		Alumno = alumno;
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

	private void CancelarCommandExecute(object obj)
	{
		CargarPerfil();

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
				MessageBox.Show("Error al habilitar la edición.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

	private async void GuardarCommandExecute(object obj)
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		switch (obj)
		{
			case "Contacto":
				messageBoxText = $"Se van a registrar los siguientes datos de cambio de contacto:\n" +
								 $"Email: {PersonaConDetalles.Email}\n" +
								 $"Teléfono: {PersonaConDetalles.Telefono}\n\n" +
								 $"¿Desea continuar?";
				caption = "Actualización de Contacto";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new CambiarContactoRequest(Alumno.PersonaID,
																 PersonaConDetalles.Telefono,
																 PersonaConDetalles.Email);
						await _servicioAlumno.ActualizarContacto(request);

						MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);

						HabilitarEditarContacto = false;
						CargarPerfil();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				break;

			case "Domicilio":
				messageBoxText = $"Se van a registrar los siguientes datos de cambio de domicilio:\n" +
								 $"Calle con Altura: {PersonaConDetalles.Calle} {PersonaConDetalles.Altura}\n" +
								 $"Vivienda: {PersonaConDetalles.Vivienda}\n" +
								 $"Observaciones: {PersonaConDetalles.Observacion}\n" +
								 $"Localidad: {PersonaConDetalles.Localidad}, {PersonaConDetalles.Provincia}, {PersonaConDetalles.Pais}";
				caption = "Actualizar domicilio";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result is MessageBoxResult.Yes)
				{
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

						MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);

						HabilitarEditarDomicilio = false;
						CargarPerfil();
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				break;

			case "Sexo":
				messageBoxText = $"Se van a registrar los siguientes datos de cambio de sexo del alumno:\n" +
								 $"Nombre Comple: {PersonaConDetalles.Apellido}, {PersonaConDetalles.Nombre}\n" +
								 $"Sexo: {PersonaConDetalles.Sexo}\n\n" +
								 $"¿Desea continuar?";
				caption = "Cambio de Sexo";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						var request = new CambiarSexoRequest(Alumno.PersonaID,
															 PersonaConDetalles.Apellido,
															 PersonaConDetalles.Nombre,
															 PersonaConDetalles.Sexo);

						await _servicioAlumno.ActualizarSexo(request);

						MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);

						HabilitarEditarSexo = false;
						CargarPerfil();
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