using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class GestionCursantesViewModel : ObservableValidator
{
	private const string INSTANCIA_POR_DEFECTO = "Parcial";

	#region Servicios
	private readonly IServicioCursante _servicioCursante;
	private readonly IServicioMateria _servicioMateria;
	private readonly IServicioCurricula _servicioCurricula;
	#endregion

	#region Stores
	private readonly CursoStore _cursoStore;
	private readonly DivisionStore _divisionStore;
	private readonly CicloLectivoStore _cicloLectivoStore;
	#endregion

	#region Dialogos
	private readonly IDialogService _dialogService;
	#endregion

	/// <summary>Ciclo lectivo con el que se obtuvo el listado vigente: es el que viaja en los requests de calificación.</summary>
	private string _periodoListado = string.Empty;

	[Required(AllowEmptyStrings=false, ErrorMessage="Debe ingresar el año para buscar el listado de alumnos de dicho ciclo lectivo.")]
	[NotifyCanExecuteChangedFor(nameof(BuscarCommandAsync))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _cicloLectivo = string.Empty;

	[NotifyCanExecuteChangedFor(nameof(NuevaCalificacionCommand))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarInasistenciaCommandAsync))]
	[ObservableProperty]
	private CursanteViewModel? _cursante;

	[ObservableProperty]
	private ObservableCollection<CursanteViewModel> _cursantes = new();

	[NotifyCanExecuteChangedFor(nameof(GuardarCalificacionCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarInasistenciaCommandAsync))]
	[ObservableProperty]
	private MateriaViewModel? _materia;

	[ObservableProperty]
	private ObservableCollection<MateriaViewModel> _materias = new();

	[NotifyCanExecuteChangedFor(nameof(GuardarCalificacionCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CancelarCalificacionCommand))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarInasistenciaCommandAsync))]
	[ObservableProperty]
	private bool _mostrarCalificacionView;

	/// <summary>Calificación en alta. El dominio no permite corregir una nota: sólo se carga una nueva.</summary>
	[ObservableProperty]
	private CalificacionViewModel? _calificacion;

	[ObservableProperty]
	private ObservableCollection<CalificacionViewModel> _calificaciones = new();

	[NotifyCanExecuteChangedFor(nameof(QuitarCalificacionCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ModificarObservacionCommandAsync))]
	[ObservableProperty]
	private CalificacionViewModel? _calificacionSeleccionada;

	/// <summary>Texto editable de la observación de la calificación seleccionada (lo único que el dominio deja modificar).</summary>
	[ObservableProperty]
	private string? _observacionEdicion;

	#region Commands
	public IAsyncRelayCommand CargarCommandAsync { get; }
	public IAsyncRelayCommand BuscarCommandAsync { get; }
	public IAsyncRelayCommand CargarCalificacionesCommandAsync { get; }
	public IAsyncRelayCommand GuardarCalificacionCommandAsync { get; }
	public IAsyncRelayCommand RegistrarInasistenciaCommandAsync { get; }
	public IAsyncRelayCommand QuitarCalificacionCommandAsync { get; }
	public IAsyncRelayCommand ModificarObservacionCommandAsync { get; }

	public IRelayCommand NuevaCalificacionCommand { get; }
	public IRelayCommand CancelarCalificacionCommand { get; }
	#endregion


	public GestionCursantesViewModel(IServicioCursante servicioCursante,
									 IServicioMateria servicioMateria,
									 IServicioCurricula servicioCurricula,
									 IDialogService dialogService,
									 CursoStore cursoStore,
									 DivisionStore divisionStore,
									 CicloLectivoStore cicloLectivoStore)
	{
		_servicioCursante = servicioCursante;
		_servicioMateria = servicioMateria;
		_servicioCurricula = servicioCurricula;
		_dialogService = dialogService;
		_cursoStore = cursoStore;
		_divisionStore = divisionStore;
		_cicloLectivoStore = cicloLectivoStore;

		CicloLectivo = _cicloLectivoStore.CicloLectivo;

		CargarCommandAsync = new AsyncRelayCommand(CargarAsync);
		BuscarCommandAsync = new AsyncRelayCommand(BuscarCursantesAsync, CanExecuteBuscar);
		CargarCalificacionesCommandAsync = new AsyncRelayCommand(CargarCalificacionesAsync);
		GuardarCalificacionCommandAsync = new AsyncRelayCommand(GuardarCalificacionAsync, CanExecuteGuardarCalificacion);
		RegistrarInasistenciaCommandAsync = new AsyncRelayCommand(RegistrarInasistenciaAsync, CanExecuteGuardarCalificacion);
		QuitarCalificacionCommandAsync = new AsyncRelayCommand(QuitarCalificacionAsync, CanExecuteCalificacionSeleccionada);
		ModificarObservacionCommandAsync = new AsyncRelayCommand(ModificarObservacionAsync, CanExecuteCalificacionSeleccionada);

		NuevaCalificacionCommand = new RelayCommand(ExecuteNuevaCalificacion, CanExecuteNuevaCalificacion);
		CancelarCalificacionCommand = new RelayCommand(ExecuteCancelarCalificacion, CanExecuteCancelarCalificacion);
	}

	#region Cambios de propiedad
	partial void OnCicloLectivoChanged(string value)
	{
		// La validación de formato (cuatro dígitos) es del dominio: acá sólo se evita guardar un valor vacío.
		if (!string.IsNullOrWhiteSpace(value))
		{
			_cicloLectivoStore.CicloLectivo = value;
		}
	}

	partial void OnCursanteChanged(CursanteViewModel? value)
	{
		Calificaciones = new ObservableCollection<CalificacionViewModel>();
		CalificacionSeleccionada = null;
		CancelarCalificacionCommand.Execute(null);

		if (value is not null)
		{
			CargarCalificacionesCommandAsync.Execute(null);
		}
	}

	partial void OnCalificacionSeleccionadaChanged(CalificacionViewModel? value)
	{
		ObservacionEdicion = value?.Observacion;
	}
	#endregion

	#region Carga
	/// <summary>Obtiene la currícula vigente del curso (la que no tiene fecha de fin) y sus materias.</summary>
	private async Task CargarAsync()
	{
		try
		{
			var curriculas = await _servicioCurricula.ListarCurriculasSegunCursoAsync(new CursoRequest(_cursoStore.Curso.CursoID));
			var vigente = curriculas.FirstOrDefault(x => x.FechaFin is null);

			if (vigente is null)
			{
				Materias = new ObservableCollection<MateriaViewModel>();
				_dialogService.MostrarAdvertencia("El curso no tiene una currícula vigente: no se pueden cargar calificaciones.", "Sin currícula vigente");
				return;
			}

			var materias = await _servicioMateria.ListarMateriasSegunCurriculaAsync(new ListarMateriasSegunCurriculaRequest(vigente.CursoID, vigente.CurriculaID));
			Materias = new ObservableCollection<MateriaViewModel>(materias.Select(x => new MateriaViewModel(x)));
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error al cargar las materias");
		}
	}

	private bool CanExecuteBuscar()
	{
		return !HasErrors && !string.IsNullOrWhiteSpace(CicloLectivo);
	}

	private async Task BuscarCursantesAsync()
	{
		try
		{
			if (_divisionStore.Division is null)
			{
				_dialogService.MostrarAdvertencia("Debe seleccionar una división para ver su listado de alumnos.", "División no seleccionada");
				return;
			}

			var request = new BuscarListadoRequest(_cursoStore.Curso.CursoID, _divisionStore.Division.DivisionID, CicloLectivo);
			var listado = await _servicioCursante.ListarCursantesAsync(request);

			_periodoListado = CicloLectivo;
			Cursantes = new ObservableCollection<CursanteViewModel>(listado.Select(x => new CursanteViewModel(x)));
			Cursante = null;
		}
		catch (FormatException ex)
		{
			// El formato del ciclo lectivo lo valida el dominio; la UI sólo informa.
			_dialogService.MostrarAdvertencia(ex.Message, "Ciclo lectivo inválido");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error al buscar el listado");
		}
	}

	private async Task CargarCalificacionesAsync()
	{
		if (Cursante is null)
		{
			return;
		}

		try
		{
			var calificaciones = await _servicioCursante.ListarCalificacionesAsync(new ListarCalificacionesRequest(Cursante.AlumnoID, _periodoListado));
			Calificaciones = new ObservableCollection<CalificacionViewModel>(calificaciones.Select(x => new CalificacionViewModel(x)));
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error al cargar las calificaciones");
		}
	}
	#endregion

	#region Alta de calificación
	private bool CanExecuteNuevaCalificacion()
	{
		return Cursante is not null;
	}

	private void ExecuteNuevaCalificacion()
	{
		Calificacion = new CalificacionViewModel(null)
		{
			Instancia = INSTANCIA_POR_DEFECTO,
		};
		Materia = null;
		MostrarCalificacionView = true;
	}

	private bool CanExecuteCancelarCalificacion()
	{
		return MostrarCalificacionView;
	}

	private void ExecuteCancelarCalificacion()
	{
		MostrarCalificacionView = false;
		Calificacion = null;
	}

	private bool CanExecuteGuardarCalificacion()
	{
		return MostrarCalificacionView && Cursante is not null && Materia is not null;
	}

	private async Task GuardarCalificacionAsync()
	{
		if (Cursante is null || Materia is null || Calificacion is null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(Calificacion.Instancia))
		{
			_dialogService.MostrarAdvertencia("Debe elegir la instancia de la calificación.", "Datos incompletos");
			return;
		}

		// Nota es double? en la UI y double en el request: sin nota no hay calificación que registrar.
		if (Calificacion.Nota is not double nota)
		{
			_dialogService.MostrarAdvertencia("Debe ingresar la nota. Si el alumno no rindió, use \"Registrar inasistencia\".", "Datos incompletos");
			return;
		}

		try
		{
			var request = new CrearCalificationRequest(AlumnoID: Cursante.AlumnoID,
													   Periodo: _periodoListado,
													   MateriaID: Materia.MateriaID,
													   Fecha: Calificacion.Fecha,
													   Instancia: Calificacion.Instancia,
													   Nota: nota,
													   Observacion: Calificacion.Observacion);
			await _servicioCursante.RegistrarCalificacionAsync(request);

			_dialogService.MostrarInformacion("La calificación se registró correctamente.", "Operación exitosa");
			CancelarCalificacionCommand.Execute(null);
			await CargarCalificacionesAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}

	private async Task RegistrarInasistenciaAsync()
	{
		if (Cursante is null || Materia is null || Calificacion is null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(Calificacion.Instancia))
		{
			_dialogService.MostrarAdvertencia("Debe elegir la instancia del examen.", "Datos incompletos");
			return;
		}

		try
		{
			var request = new RegistrarInasistenciaRequest(AlumnoID: Cursante.AlumnoID,
														   Periodo: _periodoListado,
														   MateriaID: Materia.MateriaID,
														   Fecha: Calificacion.Fecha,
														   Instancia: Calificacion.Instancia,
														   Observacion: Calificacion.Observacion);
			await _servicioCursante.RegistrarInasistenciaAExamenAsync(request);

			_dialogService.MostrarInformacion("La inasistencia al examen se registró correctamente.", "Operación exitosa");
			CancelarCalificacionCommand.Execute(null);
			await CargarCalificacionesAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}
	#endregion

	#region Calificaciones existentes
	private bool CanExecuteCalificacionSeleccionada()
	{
		return CalificacionSeleccionada is not null && Cursante is not null;
	}

	private async Task QuitarCalificacionAsync()
	{
		if (Cursante is null || CalificacionSeleccionada is null)
		{
			return;
		}

		if (!_dialogService.Confirmar("¿Desea quitar la calificación seleccionada? Para corregir una nota hay que quitarla y volver a cargarla.", "Quitar calificación"))
		{
			return;
		}

		try
		{
			await _servicioCursante.QuitarCalificacionAsync(new EliminarCalificacionRequest(Cursante.AlumnoID, _periodoListado, CalificacionSeleccionada.CalificacionID));

			_dialogService.MostrarInformacion("La calificación se quitó correctamente.", "Operación exitosa");
			await CargarCalificacionesAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}

	private async Task ModificarObservacionAsync()
	{
		if (Cursante is null || CalificacionSeleccionada is null)
		{
			return;
		}

		try
		{
			await _servicioCursante.ModificarObservacionCalificacionAsync(new ModificarObservacionCalificacionRequest(Cursante.AlumnoID,
																													  _periodoListado,
																													  CalificacionSeleccionada.CalificacionID,
																													  ObservacionEdicion));

			_dialogService.MostrarInformacion("La observación se modificó correctamente.", "Operación exitosa");
			await CargarCalificacionesAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}
	#endregion
}
