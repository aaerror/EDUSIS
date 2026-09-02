using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Responses;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout;
using System.Windows;
using System;
using WPF_Desktop.ViewModels.Docentes.Puestos;
using WPF_Desktop.ViewModels.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

internal partial class RegistrarDocenteViewModel : ObservableValidator
{
	private readonly IServicioDocente _servicioDocentes;

	#region ViewModels
	[ObservableProperty]
	private InformacionPersonalViewModel _informacionPersonal;

	[ObservableProperty]
	private ContactoViewModel _contacto;

	[ObservableProperty]
	private DomicilioViewModel _domicilio;

	[ObservableProperty]
	private LegajoDocenteViewModel _legajoDocente;

	[ObservableProperty]
	private PuestoDocenteViewModel _puestoDocente;
	#endregion

	[ObservableProperty]
	private int _tab = 0;

	#region Commands
	public IRelayCommand GuardarCommand { get; }
	public IRelayCommand ContinuarCommand { get; }
	public IRelayCommand AtrasCommand { get; }
	#endregion


	public RegistrarDocenteViewModel(IServicioDocente servicioDocentes)
	{
		_servicioDocentes = servicioDocentes;

		_informacionPersonal = new();
		_contacto = new(null);
		_domicilio = new(null);
		_legajoDocente = new(null);
		_puestoDocente = new();

		AtrasCommand = new RelayCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
		ContinuarCommand = new RelayCommand(ExecuteContinuarCommand, CanExecuteContinuarCommand);
		GuardarCommand = new RelayCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
	}

	#region GuardarCommand
	private bool CanExecuteGuardarCommand() =>
		!HasErrors && !InformacionPersonal.HasErrors && !Domicilio.HasErrors && !Contacto.HasErrors;

	private async void ExecuteGuardarCommand()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		if (string.IsNullOrWhiteSpace(LegajoDocente.Legajo) || string.IsNullOrWhiteSpace(LegajoDocente.PrefijoCuil) || string.IsNullOrWhiteSpace(LegajoDocente.PosfijoCuil))
		{
			messageBoxText = "Se deben ingresar los datos correspondientes para continuar.";
			caption = "Error en la operación";

			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

			LegajoDocente.Legajo = string.Empty;
			LegajoDocente.PrefijoCuil = string.Empty;
			LegajoDocente.PosfijoCuil = string.Empty;
		}
		else
		{
			messageBoxText = $"¿Está seguro que desea registrar los datos del docente {InformacionPersonal.Apellido}, {InformacionPersonal.Nombre}?";
			caption = "Registrar Docente";

			result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result is MessageBoxResult.Yes)
			{
				var requestDatosPersonales = new RegistrarDatosPersonalesRequest(
					Apellido: InformacionPersonal.Apellido,
					Nombre: InformacionPersonal.Nombre,
					Documento: InformacionPersonal.DocumentoNacionalIdentidad,
					Sexo: InformacionPersonal.Sexo.ToString(),
					FechaNacimiento: InformacionPersonal.FechaNacimiento,
					Nacionalidad: InformacionPersonal.Nacionalidad);

				var requestDomicilio = new RegistrarDomicilioRequest(
					Calle: Domicilio.Calle,
					Altura: Domicilio.Altura,
					Vivienda: Domicilio.Vivienda.ToString(),
					Observacion: Domicilio.Observaciones,
					Localidad: Domicilio.Localidad,
					Provincia: Domicilio.Provincia,
					Pais: Domicilio.Pais);

				var requestContacto = new RegistrarContactoRequest(
					Telefono: Contacto.Telefono,
					Email: Contacto.Email);

				var requestPuesto = new RegistrarPuestoDocenteRequest(
					DocenteID: Guid.Empty,
					Posicion: PuestoDocente.Posicion,
					Estado: PuestoDocente.Estado,
					FechaInicio: PuestoDocente.FechaInicio,
					FechaFin: PuestoDocente.FechaFin);

				var request = new RegistrarDocenteRequest(
					Legajo: LegajoDocente.Legajo,
					CUIL: LegajoDocente.CodigoUnicoIdentificacionLaboral,
					FechaAlta: LegajoDocente.FechaAlta,
					DatosPersonales: requestDatosPersonales,
					Domicilio: requestDomicilio,
					Contacto: requestContacto,
					Puesto: requestPuesto);

				try
				{
					/*if (!_servicioDocentes.EsDNIValido(new VerificarDNIRequest(request.DNI)))
					{
						messageBoxText = $"El número de documento, {request.DNI}, ya se encuentra registrado con otro docente.";
						caption = "Error en la operación";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

						InformacionPersonal.DNI = string.Empty;
						Tab = 0;

						return;
					}

					if (!_servicioDocentes.EsLegajoValido(new VerificarLegajoRequest(request.Legajo)))
					{
						messageBoxText = $"El legajo docente, {request.Legajo}, ya se encuentra registrado con otro docente.";
						caption = "Error en la operación";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

						DocenteInstitucionalViewModel.Legajo = string.Empty;

						return;
					}

					if (!_servicioDocentes.EsCUILValido(new VerificarCuilRequest(request.CUIL)))
					{
						messageBoxText = $"El CUIL del docente, {request.Apellido} {request.Nombre}, ya se encuentra registrado con otro personal de la institución.";
						caption = "Error en la operación";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

						DocenteInstitucionalViewModel.PrefijoCuil = string.Empty;
						DocenteInstitucionalViewModel.PosfijoCuil = string.Empty;

						return;
					}*/

					await _servicioDocentes.RegistrarDocenteAsync(request);

					messageBoxText = $"Se han registrado los datos de un nuevo docente.";
					caption = "Operación Exitosa";
					MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

					InformacionPersonal = new InformacionPersonalViewModel(null);
					Domicilio = new DomicilioViewModel(null);
					Contacto = new ContactoViewModel(null);
					LegajoDocente = new LegajoDocenteViewModel(null);
					PuestoDocente = new PuestoDocenteViewModel(null);

					Tab = 0;
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
	}
	#endregion

	#region ContinuarCommand
	private bool CanExecuteContinuarCommand() =>
		!InformacionPersonal.HasErrors && !Domicilio.HasErrors && !Contacto.HasErrors;

	private void ExecuteContinuarCommand()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		if (string.IsNullOrWhiteSpace(InformacionPersonal.Apellido) || string.IsNullOrWhiteSpace(InformacionPersonal.Nombre) || string.IsNullOrWhiteSpace(InformacionPersonal.DocumentoNacionalIdentidad) || string.IsNullOrWhiteSpace(Domicilio.Calle) || string.IsNullOrWhiteSpace(Domicilio.Localidad) || string.IsNullOrWhiteSpace(Domicilio.Provincia) || string.IsNullOrWhiteSpace(Contacto.Telefono) || string.IsNullOrWhiteSpace(Contacto.Email))
		{
			messageBoxText = "Se deben ingresar los datos correspondientes para continuar.";
			caption = "Error en la operación";

			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

			InformacionPersonal = new InformacionPersonalViewModel(
				new DatosPersonalesResponse(
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					DateTime.Now,
					string.Empty));

			Domicilio = new DomicilioViewModel(
				new DomicilioResponse(
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty));

			Contacto = new ContactoViewModel(
				new ContactoResponse(
					string.Empty,
					string.Empty));

			return;
		}

		LegajoDocente.NombreCompleto = InformacionPersonal.Apellido + ", " + InformacionPersonal.Nombre;
		LegajoDocente.DocumentoNacionalIdentidad = InformacionPersonal.DocumentoNacionalIdentidad;
		Tab = 1;
	}
	#endregion

	#region AtrasCommand
	private bool CanExecuteAtrasCommand() =>
		!LegajoDocente.HasErrors && !PuestoDocente.HasErrors;

	private void ExecuteAtrasCommand() =>
		Tab = 0;
	#endregion

	private void GenerateDocument(string filename)
	{
		var writer = new PdfWriter(filename);
		var pdf = new PdfDocument(writer);
		var document = new Document(pdf);
		document.Add(new Paragraph(""));

		writer.Close();
	}
}