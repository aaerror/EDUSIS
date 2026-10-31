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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Shared;

namespace WPF_Desktop.ViewModels.Alumnos;

internal partial class RegistrarAlumnoViewModel : ObservableValidator, IUbicacion
{
	private readonly IServicioAlumno _servicioAlumnos;
	private readonly IServicioCurso _servicioCursos;
	private readonly IServicioCursante _servicioCursantes;
	private readonly IServicioDivision _servicioDivisiones;
	private readonly IDialogService _dialogService;
	private readonly CicloLectivoStore _cicloLectivoStore;

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

	/// <summary>
	/// Fecha de inicio de la cursada: la ingresa el usuario, con la de hoy precargada.
	/// </summary>
	[ObservableProperty]
	private DateTime _fechaInicio = DateTime.Today;

	[ObservableProperty]
	private int _tab = 0;


	public ObservableCollection<CursoViewModel> Cursos { get; } = new();
	public ObservableCollection<DivisionViewModel> Divisiones { get; } = new();

	#region Commands
	public IRelayCommand AtrasCommand { get; }
	public IAsyncRelayCommand ContinuarCommand { get; }
	public IAsyncRelayCommand GuardarCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion

	public IReadOnlyList<Migaja> Ruta { get; }

	#region AsyncCommands
	public IAsyncRelayCommand ActualizarCursosAsyncCommand; 
	#endregion


	public RegistrarAlumnoViewModel(IServicioAlumno servicioAlumnos,
									IServicioCurso servicioCursos,
									IServicioCursante servicioCursantes,
									IServicioDivision servicioDivisiones,
									IDialogService dialogService,
									INavigationService gestionAlumnosNavigationService,
									CicloLectivoStore cicloLectivoStore)
	{
		_servicioAlumnos = servicioAlumnos;
		_dialogService = dialogService;
		_servicioCursos = servicioCursos;
		_servicioCursantes = servicioCursantes;
		_servicioDivisiones = servicioDivisiones;
		_cicloLectivoStore = cicloLectivoStore;

		_informacionPersonal = new(null);
		_domicilio = new(null);
		_contacto = new(null);

		VolverCommand = new RelayCommand(gestionAlumnosNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Hacia("ALUMNOS", VolverCommand),
			Migaja.Actual("ALTA")
		};

		AtrasCommand = new RelayCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
		ContinuarCommand = new AsyncRelayCommand(ExecuteContinuarCommand, CanExecuteContinuarCommand);
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
			_dialogService.MostrarError(ex.Message, "No se pudieron cargar los cursos");
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
			_dialogService.MostrarError(ex.Message, "No se pudieron cargar las divisiones del curso");
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

	/// <summary>
	/// Paso 1: da de alta la persona y pasa al paso de la inscripción.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Tres cosas cambiaron. Era <c>async void</c>, así que una excepción fuera del try apagaba
	/// la aplicación en medio de un alta. Si faltaba un campo, reemplazaba los tres ViewModels
	/// del paso por instancias vacías: avisaba de un dato faltante y borraba todos los demás.
	/// Y además el aviso decía "Error al Registrar un Docente" en la pantalla de alumnos.
	/// </para>
	/// <para>
	/// También se fue el "¿Está seguro que desea registrar…?": el botón dice "Continuar" y lo
	/// que hace es seguir; preguntarlo de nuevo no agregaba nada.
	/// </para>
	/// </remarks>
	private async Task ExecuteContinuarCommand()
	{
		var faltantes = new List<string>();

		if (string.IsNullOrWhiteSpace(InformacionPersonal.Apellido)) { faltantes.Add("apellido"); }
		if (string.IsNullOrWhiteSpace(InformacionPersonal.Nombre)) { faltantes.Add("nombre"); }
		if (string.IsNullOrWhiteSpace(InformacionPersonal.DocumentoNacionalIdentidad)) { faltantes.Add("documento"); }
		if (string.IsNullOrWhiteSpace(Domicilio.Calle)) { faltantes.Add("calle"); }
		if (string.IsNullOrWhiteSpace(Domicilio.Localidad)) { faltantes.Add("localidad"); }
		if (string.IsNullOrWhiteSpace(Domicilio.Provincia)) { faltantes.Add("provincia"); }
		if (string.IsNullOrWhiteSpace(Contacto.Telefono)) { faltantes.Add("teléfono"); }
		if (string.IsNullOrWhiteSpace(Contacto.Email)) { faltantes.Add("correo"); }

		if (faltantes.Count > 0)
		{
			_dialogService.MostrarAdvertencia($"Falta completar: { string.Join(", ", faltantes) }.", "Datos incompletos");

			return;
		}

		try
		{
			string documento = InformacionPersonal.DocumentoNacionalIdentidad;

			var esDocumentoInvalido = await _servicioAlumnos.EsDocumentoInvalidoAsync(new DocumentoRequest(documento));
			if (esDocumentoInvalido)
			{
				_dialogService.MostrarAdvertencia($"El documento { documento } ya está registrado con otra persona. Revisá el número o buscá el alumno en el índice.", "Documento repetido");

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

			Tab = 1;

			_dialogService.MostrarInformacion($"{ InformacionPersonal.Apellido }, { InformacionPersonal.Nombre }. Falta inscribirlo en una división.", "Alumno registrado");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo registrar el alumno");
		}
	}
	#endregion

	#region GuardarCommand
	private bool CanExecuteGuardarCommand() =>
		!HasErrors && Curso is not null && Division is not null;

	/// <summary>
	/// Paso 2: inscribe el alumno recién registrado en una división.
	/// </summary>
	/// <remarks>
	/// La confirmación se queda, y es de las pocas que vale la pena: el alumno queda inscripto
	/// en un curso, una división y un ciclo lectivo determinados, y moverlo después es otro
	/// trámite —"Cambiar de división" en la pantalla de cursantes—. El diálogo repite esos tres
	/// datos porque son los que no se pueden corregir con un clic.
	///
	/// Lo que se fue: el "Periodo = string.Empty" que borraba el ciclo lectivo cargado cuando
	/// el aviso era justamente que faltaba el ciclo lectivo.
	/// </remarks>
	private async Task ExecuteGuardarCommandAsync()
	{
		if (Curso is null || Division is null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(Periodo))
		{
			_dialogService.MostrarAdvertencia("Indicá el ciclo lectivo de la inscripción.", "Falta el ciclo lectivo");

			return;
		}

		if (alumnoID.Equals(Guid.Empty))
		{
			Tab = 0;
			_dialogService.MostrarAdvertencia("Primero completá el paso de datos personales para dar de alta el alumno.", "El alumno todavía no está registrado");

			return;
		}

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
				titulo: $"Inscribir a { InformacionPersonal.Apellido }, { InformacionPersonal.Nombre }",
				mensaje: $"Curso: { Curso.Grado }° año ({ Curso.NivelEducativo }). "
					   + $"División: { Division.Descripcion }. Ciclo lectivo: { Periodo }. "
					   + "Para moverlo después hay que darlo de baja y cambiarlo de división desde Cursantes.",
				verbo: "Inscribir alumno")))
		{
			return;
		}

		try
		{
			// El cupo y la inscripción duplicada los valida Core: acá sólo se muestra la excepción.
			var request = new RegistrarCursanteRequest(Curso.CursoID, Division.DivisionID, alumnoID, Periodo, FechaInicio);
			await _servicioCursantes.InscribirCursanteAsync(request);

			string inscripto = $"{ InformacionPersonal.Apellido }, { InformacionPersonal.Nombre }";
			string destino = $"{ Curso.Grado }° { Division.Descripcion } ({ Periodo })";

			alumnoID = Guid.Empty;
			InformacionPersonal = new(null);
			Domicilio = new(null);
			Contacto = new(null);

			Curso = null;
			FechaInicio = DateTime.Today;
			Periodo = _cicloLectivoStore.CicloLectivo;

			await ActualizarCursos();

			Tab = 0;

			_dialogService.MostrarInformacion($"{ inscripto } quedó inscripto en { destino }.", "Alumno inscripto");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo inscribir el alumno");
		}
	}
	#endregion
}
