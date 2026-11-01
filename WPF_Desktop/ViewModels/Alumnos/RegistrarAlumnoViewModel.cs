using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos;
using Core.ServicioCursos;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Shared;
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
	private readonly IDialogService _dialogService;

	#region ViewModels
	// Los tres se enganchan a ErrorsChanged para reevaluar ContinuarCommand: ver SuscribirValidacion.
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
	#endregion

	#region AsyncCommands
	public IAsyncRelayCommand ActualizarCursosAsyncCommand { get; }
	#endregion


	public RegistrarAlumnoViewModel(IServicioAlumno servicioAlumnos, IServicioCurso servicioCursos, IServicioCursante servicioCursantes, IServicioDivision servicioDivisiones, CicloLectivoStore cicloLectivoStore, IDialogService dialogService)
	{
		_servicioAlumnos = servicioAlumnos;
		_servicioCursos = servicioCursos;
		_servicioCursantes = servicioCursantes;
		_servicioDivisiones = servicioDivisiones;
		_cicloLectivoStore = cicloLectivoStore;
		_dialogService = dialogService;

		_informacionPersonal = new(null);
		_domicilio = new(null);
		_contacto = new(null);

		AtrasCommand = new RelayCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
		ContinuarCommand = new AsyncRelayCommand(ExecuteContinuarCommandAsync, CanExecuteContinuarCommand);
		GuardarCommand = new AsyncRelayCommand(ExecuteGuardarCommandAsync, CanExecuteGuardarCommand);

		ActualizarCursosAsyncCommand = new AsyncRelayCommand(ActualizarCursos);

		// Después de crear los comandos: SuscribirValidacion notifica a ContinuarCommand, que acá
		// todavía no existiría.
		SuscribirValidacion(_informacionPersonal);
		SuscribirValidacion(_domicilio);
		SuscribirValidacion(_contacto);

		// Las propiedades con [NotifyCanExecuteChangedFor] se asignan DESPUÉS de crear los comandos.
		Periodo = _cicloLectivoStore.CicloLectivo;

		_ = ActualizarCursos();
	}

	#region Reevaluación de ContinuarCommand
	// CanExecuteContinuarCommand lee HasErrors de los tres sub-ViewModels, y esos cambian cuando el
	// usuario tipea DENTRO de ellos, no cuando se reasigna la propiedad del padre. Un
	// [NotifyCanExecuteChangedFor] sobre InformacionPersonal/Domicilio/Contacto sólo dispararía al
	// cambiar la instancia entera, así que el botón quedaba con el estado de la última reasignación.
	// La suscripción a ErrorsChanged es lo que lo mantiene al día.
	private void SuscribirValidacion(ObservableValidator? validable)
	{
		if (validable is not null)
		{
			validable.ErrorsChanged += AlCambiarLosErrores;
		}
	}

	private void DesuscribirValidacion(ObservableValidator? validable)
	{
		if (validable is not null)
		{
			validable.ErrorsChanged -= AlCambiarLosErrores;
		}
	}

	private void AlCambiarLosErrores(object? remitente, DataErrorsChangedEventArgs argumentos) =>
		ContinuarCommand.NotifyCanExecuteChanged();

	// El overload de dos parámetros permite soltar el handler de la instancia vieja; sin eso cada
	// reasignación (las de ExecuteGuardarCommandAsync al limpiar el formulario) dejaría un
	// suscriptor colgado del objeto anterior.
	partial void OnInformacionPersonalChanged(InformacionPersonalViewModel? oldValue, InformacionPersonalViewModel newValue)
	{
		DesuscribirValidacion(oldValue);
		SuscribirValidacion(newValue);
		ContinuarCommand.NotifyCanExecuteChanged();
	}

	partial void OnDomicilioChanged(DomicilioViewModel? oldValue, DomicilioViewModel newValue)
	{
		DesuscribirValidacion(oldValue);
		SuscribirValidacion(newValue);
		ContinuarCommand.NotifyCanExecuteChanged();
	}

	partial void OnContactoChanged(ContactoViewModel? oldValue, ContactoViewModel newValue)
	{
		DesuscribirValidacion(oldValue);
		SuscribirValidacion(newValue);
		ContinuarCommand.NotifyCanExecuteChanged();
	}
	#endregion

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
			_dialogService.MostrarError(ex.Message, "Error al cargar los cursos");
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
			_dialogService.MostrarError(ex.Message, "Error al cargar las divisiones");
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

	private async Task ExecuteContinuarCommandAsync()
	{
		// Sexo y Vivienda entran en la guarda aunque no sean campos de texto: los dos salen de un
		// combo que arranca sin selección, quedan en null con un response nulo, y más abajo se les
		// llama .ToString(). Sin chequearlos acá, no elegir en esos dos combos no daba el aviso de
		// campos incompletos sino un NullReferenceException, que además se perdía porque el comando
		// anterior no devolvía Task y nadie podía esperarlo (UI-33).
		if (string.IsNullOrWhiteSpace(InformacionPersonal.Apellido) || string.IsNullOrWhiteSpace(InformacionPersonal.Nombre) || string.IsNullOrWhiteSpace(InformacionPersonal.DocumentoNacionalIdentidad) || string.IsNullOrWhiteSpace(InformacionPersonal.Sexo) || string.IsNullOrWhiteSpace(Domicilio.Calle) || string.IsNullOrWhiteSpace(Domicilio.Vivienda) || string.IsNullOrWhiteSpace(Domicilio.Localidad) || string.IsNullOrWhiteSpace(Domicilio.Provincia) || string.IsNullOrWhiteSpace(Contacto.Telefono) || string.IsNullOrWhiteSpace(Contacto.Email))
		{
			// Sólo se avisa: lo que el usuario ya cargó NO se toca. Antes acá se reemplazaban los
			// tres sub-ViewModels por instancias vacías, así que olvidarse un teléfono borraba
			// también el nombre, el DNI y el domicilio ya tipeados.
			_dialogService.MostrarError("Se deben ingresar los datos correspondientes para continuar.",
										"Error al registrar un alumno");

			return;
		}

		if (!_dialogService.Confirmar($"¿Está seguro que desea registrar los datos del alumno { InformacionPersonal.Apellido }, { InformacionPersonal.Nombre }?",
									  "Registrar alumno"))
		{
			return;
		}

		try
		{
			var documento = InformacionPersonal.DocumentoNacionalIdentidad;

			var esDocumentoInvalido = await _servicioAlumnos.EsDocumentoInvalidoAsync(new DocumentoRequest(documento));
			if (esDocumentoInvalido)
			{
				_dialogService.MostrarAdvertencia($"El número de documento ({ documento }) ya se encuentra registrado con otra persona.",
												  "Documento duplicado");

				InformacionPersonal.DocumentoNacionalIdentidad = string.Empty;

				return;
			}

			var request = new RegistrarAlumnoRequest(
				Apellido: InformacionPersonal.Apellido,
				Nombre: InformacionPersonal.Nombre,
				DNI: documento,
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

			_dialogService.MostrarInformacion($"Se registraron los datos del alumno { InformacionPersonal.Apellido }, { InformacionPersonal.Nombre }.",
											  "Operación exitosa");

			Tab = 1;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error al registrar un alumno");
		}
	}
	#endregion

	#region GuardarCommand
	private bool CanExecuteGuardarCommand() =>
		!HasErrors && Curso is not null && Division is not null;

	private async Task ExecuteGuardarCommandAsync()
	{
		if (Curso is null || Division is null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(Periodo))
		{
			_dialogService.MostrarError("Se deben completar todos los campos necesarios para poder registrar el alumno en el curso.",
										"Error al inscribir");

			Periodo = string.Empty;

			return;
		}

		if (alumnoID.Equals(Guid.Empty))
		{
			_dialogService.MostrarError("Antes de inscribir el alumno en el curso debe estar registrado previamente.",
										"Error al inscribir");

			Tab = 0;

			return;
		}

		var confirmacion = $"Está a punto de inscribir a { InformacionPersonal.Apellido } { InformacionPersonal.Nombre } en:\n" +
						   $"\tCurso: { Curso.Grado }° Año ({ Curso.NivelEducativo })\n"+
						   $"\tDivisión: { Division.Descripcion }\n"+
						   $"\tCiclo Lectivo: { Periodo }\n\n"+
						   $"¿Desea continuar?";
		if (!_dialogService.Confirmar(confirmacion, "Inscripción del alumno"))
		{
			return;
		}

		try
		{
			// El cupo y la inscripción duplicada los valida Core: acá sólo se muestra la excepción.
			var request = new RegistrarCursanteRequest(Curso.CursoID, Division.DivisionID, alumnoID, Periodo, FechaInicio);
			await _servicioCursantes.InscribirCursanteAsync(request);

			_dialogService.MostrarInformacion("El alumno se inscribió en el curso correctamente.",
											  "Operación exitosa");

			alumnoID = Guid.Empty;
			InformacionPersonal = new(null);
			Domicilio = new(null);
			Contacto = new(null);

			Curso = null;
			FechaInicio = DateTime.Today;
			Periodo = _cicloLectivoStore.CicloLectivo;

			await ActualizarCursos();

			Tab = 0;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error al inscribir");
		}
	}
	#endregion
}
