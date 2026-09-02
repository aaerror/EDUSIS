using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos;
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Responses;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Shared;

namespace WPF_Desktop.ViewModels.Alumnos;

//TODO: Refactorizar
internal partial class RegistrarAlumnoViewModel : ObservableValidator
{
	private readonly IServicioAlumno _servicioAlumnos;
	private readonly IServicioCurso _servicioCursos;

	#region Request
	private RegistrarCursanteRequest _crearCursanteRequest;
	private RegistrarDatosPersonalesRequest _informacionPersonalRequest;
	private RegistrarContactoRequest _contactoRequest;
	private RegistrarDomicilioRequest _domicilioRequest;
	#endregion

	#region ViewModels
	[ObservableProperty]
	private InformacionPersonalViewModel _informacionPersonal;

	[ObservableProperty]
	private DomicilioViewModel _domicilio;

	[ObservableProperty]
	private ContactoViewModel _contacto;

	[ObservableProperty]
	private CursoViewModel _curso;

	[ObservableProperty]
	private DivisionViewModel _division;
	#endregion

	private Guid alumnoID = Guid.Empty;

	[Required(AllowEmptyStrings=false, ErrorMessage="Debe ingresar el año en el que desea inscribir el alumno.")]
	[RegularExpression(@"^(20)\d{2}$", ErrorMessage="Formato inválido del año. Puede ingresar un año comprendido entre el 2000 y el 2099.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _periodo;

	[ObservableProperty]
	private int _tab = 0;


	private ObservableCollection<CursoViewModel> _cursos = new();
	private ObservableCollection<DivisionViewModel> _divisiones = new();

	#region Commands
	public IRelayCommand AtrasCommand { get; }
	public IRelayCommand ContinuarCommand { get; }
	public IRelayCommand GuardarCommand { get; }
	#endregion

	#region AsyncCommands
	public IAsyncRelayCommand ActualizarCursosAsyncCommand; 
	#endregion


	public RegistrarAlumnoViewModel(IServicioAlumno servicioAlumnos, IServicioCurso servicioCursos)
	{
		_servicioAlumnos = servicioAlumnos;
		_servicioCursos = servicioCursos;

		_informacionPersonal = new(null);
		_domicilio = new(null);
		_contacto = new(null);

		AtrasCommand = new RelayCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
		ContinuarCommand = new RelayCommand(ExecuteContinuarCommand, CanExecuteContinuarCommand);
		GuardarCommand = new RelayCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);

		ActualizarCursosAsyncCommand = new AsyncRelayCommand(ActualizarCursos);
	}

	private async Task ActualizarCursos()
	{
		var cursos = await _servicioCursos.ListarCursosAsync();
		_cursos = new ObservableCollection<CursoViewModel>(cursos.Select(x => new CursoViewModel(x)));
	}

	#region TODO:
	/*
	public CursoViewModel CursoViewModel
	{
		get
		{
			return _cursoViewModel;
		}

		set
		{
			_cursoViewModel = value;
			SetProperty(ref _cursoViewModel, value);

			if (CursoViewModel is not null)
			{
				var divisiones = _servicioCursos.BuscarDivisiones(CursoViewModel.CursoID);
				Divisiones = new ObservableCollection<DivisionViewModel>(divisiones.Select(x => new DivisionViewModel(x)));
			}
			
		}
	}

	public DivisionViewModel DivisionViewModel
	{
		get
		{
			return _divisionViewModel;
		}

		set
		{
			_divisionViewModel = value;
			SetProperty(ref _divisionViewModel, value);
		}
	}

	public ObservableCollection<CursoViewModel> Cursos
	{
		get
		{
			return _cursos;
		}
	}

	public ObservableCollection<DivisionViewModel> Divisiones
	{
		get
		{
			return _divisiones;
		}

		set
		{
			_divisiones = value;
			SetProperty(ref _divisiones, value);
		}
	}
	*/
	#endregion

	#region AtrasCommand
	private bool CanExecuteAtrasCommand() =>
		true;

	private void ExecuteAtrasCommand() =>
		Tab = 0;
	#endregion

	#region ContinuarCommand
	private bool CanExecuteContinuarCommand() =>
		!InformacionPersonal.HasErrors && !Domicilio.HasErrors && !Contacto.HasErrors;

	private async void ExecuteContinuarCommand()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		if (string.IsNullOrWhiteSpace(InformacionPersonal.Apellido) || string.IsNullOrWhiteSpace(InformacionPersonal.Nombre) || string.IsNullOrWhiteSpace(InformacionPersonal.DocumentoNacionalIdentidad) || string.IsNullOrWhiteSpace(Domicilio.Calle) || string.IsNullOrWhiteSpace(Domicilio.Localidad) || string.IsNullOrWhiteSpace(Domicilio.Provincia) || string.IsNullOrWhiteSpace(Contacto.Telefono) || string.IsNullOrWhiteSpace(Contacto.Email))
		{
			messageBoxText = "Se deben ingresar los datos correspondientes para continuar.";
			caption = "Error al Registrar un Docente";
			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

			InformacionPersonal = new InformacionPersonalViewModel(
				new DatosPersonalesResponse(
					string.Empty,
					string.Empty,
					string.Empty,
					"",
					DateTime.Now,
					string.Empty));

			Domicilio = new DomicilioViewModel(
				new DomicilioResponse(
					string.Empty,
					string.Empty,
					"",
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty));

			Contacto = new ContactoViewModel(
				new ContactoResponse(string.Empty, string.Empty));

			return;
		}
		else
		{
			messageBoxText = $"¿Está seguro que desea registrar los datos del alumno { InformacionPersonal.Apellido }, { InformacionPersonal.Nombre }?";
			caption = "Registrar Alumno";
			result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result is MessageBoxResult.Yes)
			{
				var requestInformacionPersonal = new RegistrarDatosPersonalesRequest(
					InformacionPersonal.Apellido,
					InformacionPersonal.Nombre,
					InformacionPersonal.DocumentoNacionalIdentidad,
					InformacionPersonal.Sexo.ToString(),
					InformacionPersonal.FechaNacimiento,
					InformacionPersonal.Nacionalidad);

				var requestDomicilio = new RegistrarDomicilioRequest(
					Domicilio.Calle,
					Domicilio.Altura,
					Domicilio.Vivienda.ToString(),
					Domicilio.Observaciones,
					Domicilio.Localidad,
					Domicilio.Provincia,
					Domicilio.Pais);

				var requestContacto = new RegistrarContactoRequest(Contacto.Telefono, Contacto.Email);

				try
				{
					var esDocumentoInvalido = await _servicioAlumnos.EsDocumentoInvalidoAsync(new DocumentoRequest(requestInformacionPersonal.Documento));
					if (esDocumentoInvalido)
					{
						messageBoxText = $"El número de documento ({ requestInformacionPersonal.Documento }) ya se encuentra registrado con otra persona.";
						caption = "Error";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

						InformacionPersonal.DocumentoNacionalIdentidad = string.Empty;

						return;
					}

					var request = new RegistrarAlumnoRequest(
						Apellido: InformacionPersonal.Apellido,
						Nombre: InformacionPersonal.Nombre,
						DNI: InformacionPersonal.DocumentoNacionalIdentidad,
						Sexo: InformacionPersonal.Sexo.ToString(),
						FechaNacimiento: InformacionPersonal.FechaNacimiento,
						Nacionalidad: InformacionPersonal.Nacionalidad,
						Telefono: Contacto.Telefono,
						Email: Contacto.Email,
						Calle: Domicilio.Calle,
						Altura: Domicilio.Altura,
						Vivienda: Domicilio.Vivienda.ToString(),
						Observacion: Domicilio.Observaciones,
						Localidad: Domicilio.Localidad,
						Provincia: Domicilio.Provincia,
						Pais: Domicilio.Pais);
					alumnoID = await _servicioAlumnos.RegistrarAlumnoAsync(request);

					messageBoxText = $"Se han registrado los datos de un nuevo alumno.";
					caption = "Operación Exitosa";
					MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

					Tab = 1;
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}
	}
	#endregion

	#region GuardarCommand
	private bool CanExecuteGuardarCommand() =>
		!HasErrors && Curso is not null && Division is not null;

	private void ExecuteGuardarCommand()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		if (string.IsNullOrWhiteSpace(Periodo))
		{
			messageBoxText = "Se deben completar todos los campos necesarios para poder registrar el alumno en el curso.";
			caption = "Error";
			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

			Periodo = string.Empty;

			return;
		}

		if (alumnoID.Equals(Guid.Empty))
		{
			messageBoxText = "Antes de inscribir el alumno en el curso debe estar registrado previamente.";
			caption = "Error";
			MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

			Tab = 0;
		}

		messageBoxText = $"Está a punto de inscribir a { InformacionPersonal.Apellido } { InformacionPersonal.Nombre } en:\n" +
						 $"\tCurso: { Curso.Grado }° Año ({ Curso.NivelEducativo })\n"+
						 $"\tDivisión: { Division.Descripcion }\n"+
						 $"\tCiclo Lectivo: { Periodo }\n\n"+
						 $"¿Desea continuar?";
		caption = "Inscripción del Alumno";
		result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (result is MessageBoxResult.Yes)
		{
			try
			{
				_crearCursanteRequest = new RegistrarCursanteRequest(Curso.CursoID, Division.DivisionID, alumnoID, Periodo);
				//TODO: Refactorizar inscripción de alumno
				//_servicioCursos.InscribirAlumnoEnDivision(_crearCursanteRequest);

				messageBoxText = $"Se han inscripto el alumno en el curso correctamente .";
				caption = "Operación Exitosa";
				MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

				alumnoID = Guid.Empty;
				InformacionPersonal = new(null);
				Domicilio = new(null);
				Contacto = new(null);

				ActualizarCursos();

				Tab = 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
	#endregion
}
