using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCurriculas.DTOs.Requests;
using Core.ServicioCurriculas;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioCursantes;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class GestionCursantesViewModel : ObservableValidator, IUbicacion
{
	private const string INSTANCIA_POR_DEFECTO = "Parcial";

	#region Servicios
	private readonly IServicioCursante _servicioCursante;
	private readonly IServicioMateria _servicioMateria;
	private readonly IServicioCurricula _servicioCurricula;

	/// <summary>Las divisiones del curso, que son los destinos posibles de un traslado.</summary>
	private readonly IServicioDivision _servicioDivision;
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
	[NotifyCanExecuteChangedFor(nameof(GuardarCalificacionCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(QuitarCalificacionCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ModificarObservacionCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarInasistenciaCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(ModificarRecursanteCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(RegistrarBajaCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CambiarDivisionCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CargarHistorialCommandAsync))]
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

	#region Inscripción
	/// <summary>
	/// Condición de recursante en edición. Arranca con la del alumno elegido y se persiste con
	/// <c>ModificarRecursanteCommandAsync</c>: tildar el checkbox no escribe nada por sí solo.
	/// Es lo único que el dominio deja corregir de una inscripción (decisión de CUR-2).
	/// </summary>
	[ObservableProperty]
	private bool _esRecursante;

	/// <summary>Fecha de la baja. La ingresa el usuario; el dominio rechaza una futura o anterior al inicio.</summary>
	[ObservableProperty]
	private DateTime _fechaBaja = DateTime.Today;

	/// <summary>Fecha del traslado: cierra la inscripción actual y abre la del destino.</summary>
	[ObservableProperty]
	private DateTime _fechaCambio = DateTime.Today;

	[NotifyCanExecuteChangedFor(nameof(CambiarDivisionCommandAsync))]
	[ObservableProperty]
	private DivisionViewModel? _divisionDestino;

	[ObservableProperty]
	private ObservableCollection<DivisionViewModel> _divisionesDestino = new();

	[ObservableProperty]
	private ObservableCollection<InscripcionHistoricaViewModel> _historial = new();
	#endregion

	#region Commands
	public IAsyncRelayCommand CargarCommandAsync { get; }
	public IAsyncRelayCommand BuscarCommandAsync { get; }
	public IAsyncRelayCommand CargarCalificacionesCommandAsync { get; }
	public IAsyncRelayCommand GuardarCalificacionCommandAsync { get; }
	public IAsyncRelayCommand RegistrarInasistenciaCommandAsync { get; }
	public IAsyncRelayCommand QuitarCalificacionCommandAsync { get; }
	public IAsyncRelayCommand ModificarObservacionCommandAsync { get; }

	public IAsyncRelayCommand ModificarRecursanteCommandAsync { get; }
	public IAsyncRelayCommand RegistrarBajaCommandAsync { get; }
	public IAsyncRelayCommand CambiarDivisionCommandAsync { get; }
	public IAsyncRelayCommand CargarHistorialCommandAsync { get; }

	public IRelayCommand NuevaCalificacionCommand { get; }
	public IRelayCommand CancelarCalificacionCommand { get; }
	public IRelayCommand VolverCommand { get; }
	#endregion

	public IReadOnlyList<Migaja> Ruta { get; }


	public GestionCursantesViewModel(IServicioCursante servicioCursante,
									 IServicioMateria servicioMateria,
									 IServicioCurricula servicioCurricula,
									 IServicioDivision servicioDivision,
									 IDialogService dialogService,
									 INavigationService gestionDivisionesNavigationService,
									 CursoStore cursoStore,
									 DivisionStore divisionStore,
									 CicloLectivoStore cicloLectivoStore)
	{
		_servicioCursante = servicioCursante;
		_servicioMateria = servicioMateria;
		_servicioCurricula = servicioCurricula;
		_servicioDivision = servicioDivision;
		_dialogService = dialogService;
		_cursoStore = cursoStore;
		_divisionStore = divisionStore;
		_cicloLectivoStore = cicloLectivoStore;

		CargarCommandAsync = new AsyncRelayCommand(CargarAsync);
		BuscarCommandAsync = new AsyncRelayCommand(BuscarCursantesAsync, CanExecuteBuscar);
		CargarCalificacionesCommandAsync = new AsyncRelayCommand(CargarCalificacionesAsync);
		GuardarCalificacionCommandAsync = new AsyncRelayCommand(GuardarCalificacionAsync, CanExecuteGuardarCalificacion);
		RegistrarInasistenciaCommandAsync = new AsyncRelayCommand(RegistrarInasistenciaAsync, CanExecuteGuardarCalificacion);
		QuitarCalificacionCommandAsync = new AsyncRelayCommand(QuitarCalificacionAsync, CanExecuteCalificacionSeleccionada);
		ModificarObservacionCommandAsync = new AsyncRelayCommand(ModificarObservacionAsync, CanExecuteCalificacionSeleccionada);

		ModificarRecursanteCommandAsync = new AsyncRelayCommand(ModificarRecursanteAsync, CanExecuteCursanteSeleccionado);
		RegistrarBajaCommandAsync = new AsyncRelayCommand(RegistrarBajaAsync, CanExecuteCursanteSeleccionado);
		CambiarDivisionCommandAsync = new AsyncRelayCommand(CambiarDivisionAsync, CanExecuteCambiarDivision);
		CargarHistorialCommandAsync = new AsyncRelayCommand(CargarHistorialAsync, CanExecuteCursanteSeleccionado);

		NuevaCalificacionCommand = new RelayCommand(ExecuteNuevaCalificacion, CanExecuteNuevaCalificacion);
		CancelarCalificacionCommand = new RelayCommand(ExecuteCancelarCalificacion, CanExecuteCancelarCalificacion);

		// Va después de crear los comandos: el setter de CicloLectivo notifica a BuscarCommandAsync (NotifyCanExecuteChangedFor)
		// sin comprobar null, y siempre entra al bloque de cambio porque el campo arranca vacío.
		CicloLectivo = _cicloLectivoStore.CicloLectivo;

		// Esta pantalla era la única del drill-down sin ninguna salida: su hermana —la planilla
		// de asistencia de la misma división— traía un "Volver a divisiones" y acá no había
		// nada. La ruta la provee, y de paso dice de qué división son estos alumnos.
		VolverCommand = new RelayCommand(gestionDivisionesNavigationService.Navigate);

		Ruta = new[]
		{
			Migaja.Contexto("CURSOS"),
			Migaja.Hacia($"{ _cursoStore.Curso.Grado.ToUpper() } AÑO · DIVISIONES", VolverCommand),
			Migaja.Actual("ALUMNOS")
		};
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

		// El historial y el destino son del alumno elegido: se limpian al cambiar de alumno para no
		// dejar en pantalla datos del anterior. El historial no se recarga solo —es una consulta
		// aparte y se pide con su botón—, a diferencia de las calificaciones.
		Historial = new ObservableCollection<InscripcionHistoricaViewModel>();
		DivisionDestino = null;
		EsRecursante = value?.EsRecursante ?? false;
		FechaBaja = DateTime.Today;
		FechaCambio = DateTime.Today;

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

		await CargarDivisionesDestinoAsync();
	}

	/// <summary>
	/// Las divisiones del curso que pueden recibir un traslado. Va en su propio <c>try</c> y
	/// después de las materias: que falle no tiene que impedir cargar calificaciones, que es el
	/// trabajo principal de esta pantalla.
	/// </summary>
	private async Task CargarDivisionesDestinoAsync()
	{
		try
		{
			var divisiones = await _servicioDivision.ListarDivisionesAsync(new ListarDivisionesRequest(_cursoStore.Curso.CursoID, CicloLectivo));
			DivisionesDestino = new ObservableCollection<DivisionViewModel>(divisiones.Select(x => new DivisionViewModel(x)));
		}
		catch (Exception ex)
		{
			DivisionesDestino = new ObservableCollection<DivisionViewModel>();
			_dialogService.MostrarAdvertencia($"No se pudieron cargar las divisiones de destino: no se van a poder registrar traslados.\nError: { ex.Message }", "Divisiones no disponibles");
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

	#region Inscripción: recursante, baja, traslado e historial
	private bool CanExecuteCursanteSeleccionado()
	{
		return Cursante is not null;
	}

	/// <remarks>
	/// Lo único que el dominio deja corregir de una inscripción. El resto de las "modificaciones"
	/// son operaciones de ciclo de vida: la baja cierra la inscripción y el traslado la cierra y
	/// abre otra. No hay forma de editar la fecha de inicio ni la división en el lugar.
	/// </remarks>
	private async Task ModificarRecursanteAsync()
	{
		if (Cursante is null)
		{
			return;
		}

		// Se confirma aunque no borre nada: la condición de recursante sale impresa en el
		// boletín y en los reportes de trayectoria, así que marcarla por error cambia un
		// documento que la escuela entrega. Era la única escritura de la pantalla que no
		// preguntaba.
		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
				titulo: EsRecursante
					? $"Marcar a { Cursante.NombreCompleto } como recursante"
					: $"Quitar la condición de recursante de { Cursante.NombreCompleto }",
				mensaje: "La condición sale impresa en el boletín y en el reporte de trayectoria académica.",
				verbo: "Guardar condición")))
		{
			return;
		}

		try
		{
			await _servicioCursante.ModificarCondicionDeRecursanteAsync(new ModificarCondicionRecursanteRequest(Cursante.CursanteID, EsRecursante));

			await BuscarCursantesAsync();
			_dialogService.MostrarInformacion($"{ Cursante.NombreCompleto }{ (EsRecursante ? " figura como recursante." : " ya no figura como recursante.") }", "Condición guardada");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "No se pudo guardar la condición");
		}
	}

	private async Task RegistrarBajaAsync()
	{
		if (Cursante is null)
		{
			return;
		}

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Dar de baja a { Cursante.NombreCompleto }",
				consecuencia: $"La inscripción se cierra con fecha { FechaBaja.ToString("d") }. El alumno deja de figurar "
							+ "en el listado de la división y de sus planillas de asistencia; la inscripción queda como "
							+ "registro histórico con sus calificaciones.",
				verbo: "Dar de baja")))
		{
			return;
		}

		try
		{
			await _servicioCursante.RegistrarBajaDeCursanteAsync(new RegistrarBajaCursanteRequest(Cursante.CursanteID, FechaBaja));

			_dialogService.MostrarInformacion("La baja se registró correctamente.", "Operación exitosa");
			await BuscarCursantesAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}

	/// <summary>
	/// Deshabilitado cuando el destino es la división donde el alumno ya está: el dominio lo
	/// rechaza con <c>DivisionDestinoInvalidaException</c>, y es mejor no habilitar la acción que
	/// mostrar el error después.
	/// </summary>
	private bool CanExecuteCambiarDivision()
	{
		return Cursante is not null
			&& DivisionDestino is not null
			&& DivisionDestino.DivisionID != _divisionStore.Division?.DivisionID;
	}

	private async Task CambiarDivisionAsync()
	{
		if (Cursante is null || DivisionDestino is null)
		{
			return;
		}

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Pasar a { Cursante.NombreCompleto } a la división { DivisionDestino.Descripcion }",
				consecuencia: $"La inscripción actual se cierra con fecha { FechaCambio.ToString("d") } y se abre una nueva "
							+ "en la división de destino. Las calificaciones ya cargadas quedan en la inscripción cerrada "
							+ "y siguen saliendo impresas en el boletín.",
				verbo: "Cambiar de división")))
		{
			return;
		}

		try
		{
			await _servicioCursante.CambiarDeDivisionAsync(new CambiarDivisionCursanteRequest(Cursante.AlumnoID,
																							   _periodoListado,
																							   DivisionDestino.DivisionID,
																							   FechaCambio));

			_dialogService.MostrarInformacion("El traslado se registró correctamente.", "Operación exitosa");
			await BuscarCursantesAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error en la operación");
		}
	}

	private async Task CargarHistorialAsync()
	{
		if (Cursante is null)
		{
			return;
		}

		try
		{
			var inscripciones = await _servicioCursante.ConsultarHistorialDeDivisionesAsync(new ConsultarHistorialRequest(Cursante.AlumnoID));
			Historial = new ObservableCollection<InscripcionHistoricaViewModel>(inscripciones.Select(x => new InscripcionHistoricaViewModel(x)));
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, "Error al cargar el historial");
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

		if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
				titulo: $"Quitar la calificación de { CalificacionSeleccionada.Materia }",
				consecuencia: "La nota se borra de la inscripción y deja de salir en el boletín. "
							+ "Para corregir una nota hay que quitarla y volver a cargarla.",
				verbo: "Quitar calificación")))
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
