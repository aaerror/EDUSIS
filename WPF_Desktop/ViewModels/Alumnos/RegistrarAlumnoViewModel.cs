using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos;
using Core.ServicioCursos;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones;
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
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Shared;

namespace WPF_Desktop.ViewModels.Alumnos;

internal partial class RegistrarAlumnoViewModel : ObservableValidator
{
	private readonly IServicioAlumno _servicioAlumnos;
	private readonly IServicioCurso _servicioCursos;
	private readonly IServicioCursante _servicioCursantes;
	private readonly IServicioDivision _servicioDivisiones;
	private readonly CicloLectivoStore _cicloLectivoStore;

	#region Request
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

	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	[ObservableProperty]
	private CursoViewModel? _curso;

	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	[ObservableProperty]
	private DivisionViewModel? _division;
	#endregion

	private Guid alumnoID = Guid.Empty;

	[Required(AllowEmptyStrings=false, ErrorMessage="Debe ingresar el año en el que desea inscribir el alumno.")]
	[RegularExpression(@"^(20)\d{2}$", ErrorMessage="Formato inválido del año. Puede ingresar un año comprendido entre el 2000 y el 2099.")]
	[NotifyDataErrorInfo]
	[NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
	[ObservableProperty]
	private string _periodo = string.Empty;

	[ObservableProperty]
	private int _tab = 0;


	public ObservableCollection<CursoViewModel> Cursos { get; } = new();
	public ObservableCollection<DivisionViewModel> Divisiones { get; } = new();

	#region Commands
	public IRelayCommand AtrasCommand { get; }
	public IRelayCommand ContinuarCommand { get; }
	public IAsyncRelayCommand GuardarCommand { get; }
	#endregion

	#region AsyncCommands
	public IAsyncRelayCommand ActualizarCursosAsyncCommand; 
	#endregion


	public RegistrarAlumnoViewModel(IServicioAlumno servicioAlumnos, IServicioCurso servicioCursos, IServicioCursante servicioCursantes, IServicioDivision servicioDivisiones, CicloLectivoStore cicloLectivoStore)
	{
		_servicioAlumnos = servicioAlumnos;
		_servicioCursos = servicioCursos;
		_servicioCursantes = servicioCursantes;
		_servicioDivisiones = servicioDivisiones;
		_cicloLectivoStore = cicloLectivoStore;

		_informacionPersonal = new(null);
		_domicilio = new(null);
		_contacto = new(null);

		AtrasCommand = new RelayCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
		ContinuarCommand = new RelayCommand(ExecuteContinuarCommand, CanExecuteContinuarCommand);
		GuardarCommand = new AsyncRelayCommand(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);

		ActualizarCursosAsyncCommand = new AsyncRelayCommand(ActualizarCursos);

		// Las propiedades con [NotifyCanExecuteChangedFor] se asignan DESPUÉS de crear los comandos.
		Periodo = _cicloLectivoStore.CicloLectivo;

		_ = ActualizarCursos();
	}

	#region Cursos y divisiones
	private async Task ActualizarCursos()
	{
		try
		{
			var cursos = await _servicioCursos.ListarCursosAsync();

			Cursos.Clear();
			foreach (var curso in cursos)
			{
				Cursos.Add(new CursoViewModel(curso));
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	partial void OnCursoChanged(CursoViewModel? value)
	{
		Division = null;
		Divisiones.Clear();

		if (value is not null)
		{
			_ = CargarDivisionesAsync(value.CursoID);
		}
	}

	private async Task CargarDivisionesAsync(Guid cursoID)
	{
		try
		{
			var divisiones = await _servicioDivisiones.ListarDivisionesAsync(new ListarDivisionesRequest(cursoID, Periodo));

			// El usuario pudo cambiar de curso mientras la consulta estaba en vuelo.
			if (Curso is null || Curso.CursoID != cursoID)
			{
				return;
			}

			Divisiones.Clear();
			foreach (var division in divisiones)
			{
				Divisiones.Add(new DivisionViewModel(division));
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
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

	private async Task ExecuteGuardarCommandAsync()
	{
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		if (Curso is null || Division is null)
		{
			return;
		}

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

			return;
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
				// El cupo y la inscripción duplicada los valida Core: acá sólo se muestra la excepción.
				var request = new RegistrarCursanteRequest(Curso.CursoID, Division.DivisionID, alumnoID, Periodo);
				await _servicioCursantes.InscribirCursanteAsync(request);

				messageBoxText = $"Se han inscripto el alumno en el curso correctamente .";
				caption = "Operación Exitosa";
				MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

				alumnoID = Guid.Empty;
				InformacionPersonal = new(null);
				Domicilio = new(null);
				Contacto = new(null);

				Curso = null;
				Periodo = _cicloLectivoStore.CicloLectivo;

				await ActualizarCursos();

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
