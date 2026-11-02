using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioAsistencias;
using Core.ServicioAsistencias.DTOs.Requests;
using Core.ServicioAsistencias.DTOs.Responses;
using Core.ServicioCursantes;
using Core.ServicioCursantes.DTOs.Requests;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

internal partial class GestionAsistenciasViewModel : ObservableObject, IUbicacion
{
	/// <summary>Título único de todos los diálogos de la pantalla.</summary>
	private const string TITULO = "Asistencia";

	#region Vistas de la división
	/// <remarks>
	/// Las tres responden preguntas distintas sobre la misma división y por eso son vistas y no
	/// secciones apiladas: la planilla es el trabajo de hoy, el historial es qué se cargó estos días y
	/// el resumen es cómo viene el período. Apilarlas obligaría a scrollear la pantalla entera para
	/// llegar a la de abajo, y las tres quieren el alto completo para su grilla.
	///
	/// Son constantes y no un enum porque viajan como <c>ConverterParameter</c>, que es texto. Mismo
	/// mecanismo que el conmutador del alumno: ver <c>GestionCursantesViewModel</c>.
	/// </remarks>
	public const string VISTA_PLANILLA = "Planilla";
	public const string VISTA_HISTORIAL = "Historial";
	public const string VISTA_RESUMEN = "Resumen";
	#endregion

	#region Servicios
	private readonly IServicioAsistencia _servicioAsistencia;

	/// <summary>
	/// El listado de alumnos de la división: es la única fuente de los nombres de la grilla, porque
	/// <see cref="PlanillaAsistenciaResponse"/> identifica a cada cursante sólo por su identificador.
	/// </summary>
	private readonly IServicioCursante _servicioCursante;

	/// <summary>
	/// La división del drill-down: de acá salen su letra —que el <c>DivisionStore</c> no guarda— y,
	/// sobre todo, si tiene preceptor asignado. Ver <see cref="TienePreceptor"/>.
	/// </summary>
	private readonly IServicioDivision _servicioDivision;
	#endregion

	#region Navegacion
	private readonly INavigationService _gestionDivisionesNavigationService;
	#endregion

	#region Stores
	private readonly CursoStore _cursoStore;
	private readonly DivisionStore _divisionStore;
	private readonly CicloLectivoStore _cicloLectivoStore;
	#endregion

	#region Dialogos
	private readonly IDialogService _dialogService;
	#endregion

	/// <summary>
	/// La planilla vigente. Queda en un campo privado y no se expone: los <c>Response</c> de
	/// <c>Core</c> no se bindean. La vista lee las propiedades observables que se derivan de ella.
	/// </summary>
	private PlanillaAsistenciaResponse? _planilla;

	/// <summary>Fecha de trabajo. No viene por store: es el <c>DatePicker</c> de esta pantalla.</summary>
	[ObservableProperty]
	private DateTime _fecha = DateTime.Today;

	/// <summary>
	/// Elegir una fecha ES pedir su planilla.
	/// </summary>
	/// <remarks>
	/// Antes había un botón "Ver planilla" al lado del calendario, y era un paso que no decidía nada:
	/// nadie cambia la fecha para quedarse mirando la planilla de la anterior. El botón además dejaba
	/// la pantalla en un estado mentiroso —fecha nueva, planilla vieja— si uno se olvidaba de
	/// apretarlo. El <c>DatePicker</c> sólo confirma al elegir del calendario o al salir del campo, así
	/// que esto no dispara una consulta por tecla.
	/// </remarks>
	partial void OnFechaChanged(DateTime value)
	{
		if (CargarCommand.CanExecute(null))
		{
			CargarCommand.Execute(null);
		}
	}

	/// <summary>
	/// Hay planilla abierta para <see cref="Fecha"/>. En <c>false</c> la pantalla queda en estado
	/// "sin planilla": grilla vacía, aviso visible y nada más que "Abrir planilla" habilitado.
	/// </summary>
	[NotifyCanExecuteChangedFor(nameof(AbrirPlanillaCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarPresenteCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarAusenciaCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarInasistenciaCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarTardanzaCommand))]
	[NotifyCanExecuteChangedFor(nameof(IncorporarCursanteCommand))]
	[NotifyCanExecuteChangedFor(nameof(CerrarPlanillaCommand))]
	[NotifyCanExecuteChangedFor(nameof(ReabrirPlanillaCommand))]
	[NotifyCanExecuteChangedFor(nameof(EliminarPlanillaCommand))]
	[ObservableProperty]
	private bool _hayPlanilla;

	/// <summary>La planilla está cerrada: el dominio ya no acepta marcar ni incorporar.</summary>
	[NotifyCanExecuteChangedFor(nameof(MarcarPresenteCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarAusenciaCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarInasistenciaCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarTardanzaCommand))]
	[NotifyCanExecuteChangedFor(nameof(IncorporarCursanteCommand))]
	[NotifyCanExecuteChangedFor(nameof(CerrarPlanillaCommand))]
	[NotifyCanExecuteChangedFor(nameof(ReabrirPlanillaCommand))]
	[ObservableProperty]
	private bool _cerrada;

	[ObservableProperty]
	private ObservableCollection<RegistroAsistenciaViewModel> _registros = new();

	/// <summary>Fila elegida en la grilla: el cursante sobre el que operan los cuatro marcados.</summary>
	[NotifyCanExecuteChangedFor(nameof(MarcarPresenteCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarAusenciaCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarInasistenciaCommand))]
	[NotifyCanExecuteChangedFor(nameof(MarcarTardanzaCommand))]
	[ObservableProperty]
	private RegistroAsistenciaViewModel? _registro;

	#region Incorporacion
	/// <summary>
	/// Los alumnos de la división que todavía no tienen registro en la planilla del día: los que se
	/// inscribieron después de abrirla. Se los suma de a uno con <c>IncorporarCursanteCommand</c>.
	/// </summary>
	[ObservableProperty]
	private ObservableCollection<CursanteViewModel> _cursantesSinRegistro = new();

	[NotifyCanExecuteChangedFor(nameof(IncorporarCursanteCommand))]
	[ObservableProperty]
	private CursanteViewModel? _cursanteAIncorporar;
	#endregion

	#region Formulario de marcado
	[NotifyCanExecuteChangedFor(nameof(MarcarTardanzaCommand))]
	[ObservableProperty]
	private int _minutosTardanza = 5;

	/// <summary>Observación opcional del marcado. En blanco viaja como <c>null</c> al caso de uso.</summary>
	[ObservableProperty]
	private string _observacion = string.Empty;
	#endregion

	#region Contadores
	[ObservableProperty]
	private int _presentes;
	[ObservableProperty]
	private int _ausencias;
	[ObservableProperty]
	private int _inasistencias;
	[ObservableProperty]
	private int _tardanzas;
	#endregion

	#region Vista elegida
	/// <summary>Cuál de las tres vistas está abierta. Es una de las constantes <c>VISTA_</c>.</summary>
	[ObservableProperty]
	private string _vista = VISTA_PLANILLA;

	/// <summary>
	/// Abrir una vista ES pedir sus datos: el historial y el resumen consultan al entrar y no con un
	/// botón aparte.
	/// </summary>
	/// <remarks>
	/// Es la lección del historial del alumno, que se pedía con un botón "Ver historial" y después no
	/// había forma de cerrarlo. Colgando la consulta del conmutador, elegir otra vista es lo que la
	/// cierra. Se reconsulta cada vez —y no sólo la primera— porque las dos miran el mismo período que
	/// el usuario viene de marcar en la planilla del día: volver al resumen después de cerrar una
	/// planilla y encontrarlo sin ese día sería mentirle.
	/// </remarks>
	partial void OnVistaChanged(string value)
	{
		if (value == VISTA_HISTORIAL)
		{
			ListarPlanillasCommand.Execute(null);
		}
		else if (value == VISTA_RESUMEN)
		{
			ConsultarResumenCommand.Execute(null);
		}
	}
	#endregion

	#region Periodo del historial y del resumen
	/// <summary>
	/// Extremos del período que miran el historial y el resumen. Arrancan en el último mes.
	/// </summary>
	/// <remarks>
	/// El valor inicial no es "todo el ciclo" a propósito: una división con clases desde marzo acumula
	/// doscientas planillas, y abrir el historial con las doscientas no es mostrar más información
	/// sino esconder las de esta semana, que son las que se vienen a buscar. El período se amplía
	/// desde los atajos o a mano, pero el que no elige nada recibe algo que se puede leer.
	/// </remarks>
	[ObservableProperty]
	private DateTime _desde = DateTime.Today.AddDays(-30);

	[ObservableProperty]
	private DateTime _hasta = DateTime.Today;

	/// <summary>Las planillas ya cargadas del período, de la más reciente a la más vieja.</summary>
	[ObservableProperty]
	private ObservableCollection<PlanillaResumenViewModel> _planillas = new();

	/// <summary>Fila elegida del historial: la fecha que <c>AbrirFechaCommand</c> lleva a la planilla.</summary>
	[NotifyCanExecuteChangedFor(nameof(AbrirFechaCommand))]
	[ObservableProperty]
	private PlanillaResumenViewModel? _planillaElegida;
	#endregion

	#region Resumen del periodo
	[ObservableProperty]
	private ObservableCollection<ResumenCursanteViewModel> _resumenCursantes = new();

	[ObservableProperty]
	private int _planillasDelPeriodo;

	[ObservableProperty]
	private int _planillasAbiertas;

	[ObservableProperty]
	private int _presentesDelPeriodo;

	[ObservableProperty]
	private int _ausenciasDelPeriodo;

	[ObservableProperty]
	private int _inasistenciasDelPeriodo;

	[ObservableProperty]
	private int _tardanzasDelPeriodo;

	/// <summary>El porcentaje de asistencia de la división en el período, ya formateado.</summary>
	[ObservableProperty]
	private string _asistenciaDelPeriodo = "0%";

	/// <summary>
	/// El período tiene al menos una planilla sin cerrar. La vista lo avisa porque un resumen con
	/// planillas abiertas todavía puede cambiar: los números que muestra no son definitivos.
	/// </summary>
	public bool HayPlanillasAbiertas => PlanillasAbiertas > 0;

	partial void OnPlanillasAbiertasChanged(int value)
	{
		OnPropertyChanged(nameof(HayPlanillasAbiertas));
	}
	#endregion

	[ObservableProperty]
	private string _curso = string.Empty;
	[ObservableProperty]
	private string _nivelEducativo = string.Empty;

	#region Preceptor
	/// <summary>Letra de la división. No viene por store: la trae <c>ListarDivisionesAsync</c> con el resto.</summary>
	[ObservableProperty]
	private string _division = string.Empty;

	/// <summary>Nombre del preceptor a cargo, o vacío si el cargo está vacante.</summary>
	[ObservableProperty]
	private string _preceptor = string.Empty;

	/// <summary>
	/// La división tiene preceptor asignado. Es la precondición de <c>AbrirPlanillaAsync</c>: el
	/// dominio toma el preceptor de <c>division.Preceptor</c> y sin él rechaza la apertura con
	/// <c>SinDatosPreceptorException</c>.
	/// </summary>
	/// <remarks>
	/// Esta propiedad existe para que el usuario NO descubra esa regla chocándose con ella. Es el
	/// mismo defecto que <c>UI-DIV-1</c> arregló en la pantalla de divisiones, donde "Asignar
	/// preceptor" quedaba habilitado con el cargo ocupado y el recorrido moría en un
	/// <c>CargoPreceptorNoDisponibleException</c> mostrado como error: ver el <c>remarks</c> de
	/// <c>GestionDivisionesViewModel.CanExecuteRegistrarCommand</c>. La regla sigue viviendo en el
	/// dominio —acá no se valida nada, sólo se refleja el estado— y el aviso de la vista lleva
	/// adentro la salida: volver a divisiones y asignar el preceptor.
	/// </remarks>
	[NotifyCanExecuteChangedFor(nameof(AbrirPlanillaCommand))]
	[ObservableProperty]
	private bool _tienePreceptor;
	#endregion

	#region Avisos derivados
	/// <summary>
	/// El aviso de "todavía no hay planilla" se muestra sólo si la división TIENE preceptor.
	/// </summary>
	/// <remarks>
	/// Es estado derivado y vive acá, no en la vista, porque los dos avisos se excluyen: sin
	/// preceptor, "Abrir planilla" está deshabilitado, así que un cartel que dice "usá Abrir
	/// planilla" estaría señalando un botón que no se puede apretar. El aviso que corresponde en ese
	/// caso es el del preceptor, que lleva adentro la salida real. Expresarlo en XAML pediría un
	/// <c>MultiBinding</c> con un converter multivalor que este tema no tiene, y además la regla de
	/// cuál de los dos avisos corresponde es lógica de presentación, no de dibujo.
	/// </remarks>
	public bool MostrarAvisoSinPlanilla => !HayPlanilla && TienePreceptor;

	/// <summary>
	/// Hay planilla y todavía acepta marcas. Es lo que decide qué acciones del ciclo se muestran.
	/// </summary>
	/// <remarks>
	/// Las cuatro acciones de la planilla —abrir, cerrar, reabrir, eliminar— estaban las cuatro
	/// siempre a la vista, y en cualquier estado real tres de ellas no se podían apretar: sin planilla
	/// sólo sirve abrir, con la planilla abierta sólo cerrar o eliminar, y con la planilla cerrada sólo
	/// reabrir. Una fila de botones apagados no informa de qué se puede hacer, obliga a leerlos todos
	/// para descubrir cuál es el único vivo. Ahora cada estado muestra las suyas, y las tres banderas
	/// —<see cref="MostrarAvisoSinPlanilla"/>, ésta y <see cref="Cerrada"/>— son excluyentes.
	/// </remarks>
	public bool PlanillaAbierta => HayPlanilla && !Cerrada;

	partial void OnHayPlanillaChanged(bool value)
	{
		OnPropertyChanged(nameof(MostrarAvisoSinPlanilla));
		OnPropertyChanged(nameof(PlanillaAbierta));
	}

	partial void OnTienePreceptorChanged(bool value)
	{
		OnPropertyChanged(nameof(MostrarAvisoSinPlanilla));
	}

	partial void OnCerradaChanged(bool value)
	{
		OnPropertyChanged(nameof(PlanillaAbierta));
	}
	#endregion

	#region Commands
	public IAsyncRelayCommand CargarCommand { get; }
	public IAsyncRelayCommand AbrirPlanillaCommand { get; }

	/// <remarks>
	/// LOS CUATRO MARCADOS RECIBEN LA FILA POR PARÁMETRO y no trabajan sobre <see cref="Registro"/>.
	/// Es la regla que <c>docs/ui-ux.md</c> dejó anotada: un <c>Button</c> dentro de una celda marca el
	/// clic como manejado, así que la fila NO se selecciona al apretarlo, y un comando que operara
	/// sobre el <c>SelectedItem</c> le marcaría la asistencia a otro alumno. Los botones del pie pasan
	/// <c>{ Binding Path=Registro }</c>, que es la misma fila por otro camino.
	/// </remarks>
	public IAsyncRelayCommand<RegistroAsistenciaViewModel> MarcarPresenteCommand { get; }
	public IAsyncRelayCommand<RegistroAsistenciaViewModel> MarcarAusenciaCommand { get; }
	public IAsyncRelayCommand<RegistroAsistenciaViewModel> MarcarInasistenciaCommand { get; }
	public IAsyncRelayCommand<RegistroAsistenciaViewModel> MarcarTardanzaCommand { get; }

	public IAsyncRelayCommand IncorporarCursanteCommand { get; }
	public IAsyncRelayCommand CerrarPlanillaCommand { get; }
	public IAsyncRelayCommand ReabrirPlanillaCommand { get; }
	public IAsyncRelayCommand EliminarPlanillaCommand { get; }

	/// <summary>Abre una de las tres vistas. El parámetro es una de las constantes <c>VISTA_</c>.</summary>
	public IRelayCommand<string> VerCommand { get; }

	public IAsyncRelayCommand ListarPlanillasCommand { get; }
	public IAsyncRelayCommand ConsultarResumenCommand { get; }

	/// <summary>Mueve el período a los últimos N días. El parámetro es la cantidad de días.</summary>
	public IRelayCommand<string> VerUltimosDiasCommand { get; }

	/// <summary>Lleva la planilla elegida del historial a la vista del día.</summary>
	public IAsyncRelayCommand AbrirFechaCommand { get; }

	public IRelayCommand VolverCommand { get; }
	#endregion


	public GestionAsistenciasViewModel(INavigationService gestionDivisionesNavigationService,
									   IServicioAsistencia servicioAsistencia,
									   IServicioCursante servicioCursante,
									   IServicioDivision servicioDivision,
									   CursoStore cursoStore,
									   DivisionStore divisionStore,
									   CicloLectivoStore cicloLectivoStore,
									   IDialogService dialogService)
	{
		_gestionDivisionesNavigationService = gestionDivisionesNavigationService;
		_servicioAsistencia = servicioAsistencia;
		_servicioCursante = servicioCursante;
		_servicioDivision = servicioDivision;

		_cursoStore = cursoStore;
		_divisionStore = divisionStore;
		_cicloLectivoStore = cicloLectivoStore;
		_dialogService = dialogService;

		CargarCommand = new AsyncRelayCommand(ExecuteCargarCommand, CanExecuteCargarCommand);
		AbrirPlanillaCommand = new AsyncRelayCommand(ExecuteAbrirPlanillaCommand, CanExecuteAbrirPlanillaCommand);
		MarcarPresenteCommand = new AsyncRelayCommand<RegistroAsistenciaViewModel>(ExecuteMarcarPresenteCommand, PuedeMarcar);
		MarcarAusenciaCommand = new AsyncRelayCommand<RegistroAsistenciaViewModel>(ExecuteMarcarAusenciaCommand, PuedeMarcar);
		MarcarInasistenciaCommand = new AsyncRelayCommand<RegistroAsistenciaViewModel>(ExecuteMarcarInasistenciaCommand, PuedeMarcar);
		MarcarTardanzaCommand = new AsyncRelayCommand<RegistroAsistenciaViewModel>(ExecuteMarcarTardanzaCommand, PuedeMarcarTardanza);
		IncorporarCursanteCommand = new AsyncRelayCommand(ExecuteIncorporarCursanteCommand, CanExecuteIncorporarCursanteCommand);
		CerrarPlanillaCommand = new AsyncRelayCommand(ExecuteCerrarPlanillaCommand, CanExecuteCerrarPlanillaCommand);
		ReabrirPlanillaCommand = new AsyncRelayCommand(ExecuteReabrirPlanillaCommand, CanExecuteReabrirPlanillaCommand);
		EliminarPlanillaCommand = new AsyncRelayCommand(ExecuteEliminarPlanillaCommand, CanExecuteEliminarPlanillaCommand);
		VerCommand = new RelayCommand<string>(ExecuteVerCommand);
		ListarPlanillasCommand = new AsyncRelayCommand(ExecuteListarPlanillasCommand, CanExecuteConsultarPeriodoCommand);
		ConsultarResumenCommand = new AsyncRelayCommand(ExecuteConsultarResumenCommand, CanExecuteConsultarPeriodoCommand);
		VerUltimosDiasCommand = new RelayCommand<string>(ExecuteVerUltimosDiasCommand);
		AbrirFechaCommand = new AsyncRelayCommand(ExecuteAbrirFechaCommand, CanExecuteAbrirFechaCommand);
		VolverCommand = new RelayCommand(ExecuteVolverCommand, CanExecuteVolverCommand);

		foreach (var consulta in new IAsyncRelayCommand[] { CargarCommand, ListarPlanillasCommand, ConsultarResumenCommand, AbrirFechaCommand })
		{
			consulta.PropertyChanged += (_, argumentos) =>
			{
				if (argumentos.PropertyName == nameof(IAsyncRelayCommand.IsRunning))
				{
					OnPropertyChanged(nameof(EstaCargando));
				}
			};
		}

		if (_cursoStore.Curso is not null)
		{
			Curso = _cursoStore.Curso.Grado;
			NivelEducativo = _cursoStore.Curso.NivelEducativo;
		}

		Ruta = new[]
		{
			Migaja.Contexto("CURSOS"),
			Migaja.Hacia($"{ Curso?.ToUpper() } AÑO · DIVISIONES", VolverCommand),
			Migaja.Actual("ASISTENCIA")
		};
	}

	/// <summary>Ruta hasta la planilla del día. El "Volver a divisiones" del encabezado era su único equivalente.</summary>
	public IReadOnlyList<Migaja> Ruta { get; }

	/// <summary>
	/// Alguna de las cuatro consultas de la pantalla está en curso. Es lo que levanta el velo.
	/// </summary>
	/// <remarks>
	/// El velo colgaba sólo de <c>CargarCommand.IsRunning</c>, que era el único comando que consultaba
	/// algo cuando la pantalla tenía una sola vista. Con el historial y el resumen hay tres consultas
	/// más, y sin esto "Ver período" sobre un ciclo entero no daría ninguna señal de estar trabajando:
	/// la grilla se quedaría con los datos viejos hasta que llegara la respuesta.
	///
	/// Se arma por suscripción y no con un <c>MultiBinding</c> porque el tema no tiene un converter
	/// multivalor, y porque cuál es el conjunto de comandos que cuentan como "cargando" es una
	/// decisión del ViewModel, no del dibujo.
	/// </remarks>
	public bool EstaCargando => CargarCommand.IsRunning
							 || ListarPlanillasCommand.IsRunning
							 || ConsultarResumenCommand.IsRunning
							 || AbrirFechaCommand.IsRunning;

	#region CargarCommand
	private bool CanExecuteCargarCommand()
	{
		return _divisionStore.Division is not null;
	}

	private async Task ExecuteCargarCommand()
	{
		try
		{
			await CargarPlanillaAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}

	/// <summary>
	/// Rearma la pantalla completa para <see cref="Fecha"/>: el listado de alumnos de la división y la
	/// planilla del día, si existe. La llaman la carga inicial y todos los comandos de escritura al
	/// terminar, que es la forma de que los contadores del <c>Response</c> queden al día sin contarlos
	/// acá.
	/// </summary>
	/// <remarks>
	/// <para>
	/// EL <c>catch (NullReferenceException)</c> NO ES UN DESCUIDO. <c>ConsultarPlanillaAsync</c> no
	/// distingue "no existe planilla para esa fecha" de un error real: su helper
	/// <c>BuscarPlanillaAsync</c> lanza <c>NullReferenceException("Planilla de asistencia no
	/// encontrada.")</c> como centinela cuando el repositorio no encuentra nada. Atrapar ese tipo
	/// exacto —y sólo ese— es lo que convierte "todavía no hay planilla" en un <b>estado de pantalla</b>
	/// (aviso + botón "Abrir planilla") en lugar de un cartel de error sobre algo que es perfectamente
	/// normal a primera hora del día.
	/// </para>
	/// <para>
	/// Cualquier otra excepción sigue subiendo hasta el <c>catch</c> del comando y se muestra como
	/// error, que es lo correcto. Si algún día <c>Core</c> devuelve <c>null</c> para la planilla
	/// inexistente o le da una excepción propia (una <c>PlanillaNoEncontradaException</c>), <b>este
	/// <c>catch</c> es el único lugar a cambiar</b>.
	/// </para>
	/// </remarks>
	private async Task CargarPlanillaAsync()
	{
		if (_divisionStore.Division is null)
		{
			return;
		}

		var divisionID = _divisionStore.Division.DivisionID;

		await CargarDivisionAsync(divisionID);

		// Se pide siempre: alimenta los nombres de la grilla y la lista de incorporables.
		var cursantes = await _servicioCursante.ListarCursantesAsync(new BuscarListadoRequest(CursoID: _cursoStore.Curso.CursoID,
																							  DivisionID: divisionID,
																							  Periodo: _cicloLectivoStore.CicloLectivo));

		Dictionary<Guid, string> nombres = cursantes.ToDictionary(c => c.CursanteID, c => c.NombreCompleto);

		PlanillaAsistenciaResponse? planilla = null;
		try
		{
			planilla = await _servicioAsistencia.ConsultarPlanillaAsync(new ConsultarPlanillaRequest(DivisionID: divisionID,
																									 Fecha: Fecha));
		}
		catch (NullReferenceException)
		{
			planilla = null;
		}

		_planilla = planilla;

		Registros.Clear();
		CursantesSinRegistro.Clear();

		if (_planilla is null)
		{
			HayPlanilla = false;
			Cerrada = false;

			Presentes = 0;
			Ausencias = 0;
			Inasistencias = 0;
			Tardanzas = 0;
		}
		else
		{
			foreach (var registro in _planilla.Registros)
			{
				var nombre = nombres.TryGetValue(registro.CursanteID, out var encontrado) ? encontrado : string.Empty;
				Registros.Add(new RegistroAsistenciaViewModel(registro, nombre));
			}

			HashSet<Guid> conRegistro = _planilla.Registros.Select(r => r.CursanteID).ToHashSet();

			foreach (var cursante in cursantes.Where(c => !conRegistro.Contains(c.CursanteID)))
			{
				CursantesSinRegistro.Add(new CursanteViewModel(cursante));
			}

			Presentes = _planilla.Presentes;
			Ausencias = _planilla.Ausencias;
			Inasistencias = _planilla.Inasistencias;
			Tardanzas = _planilla.Tardanzas;

			Cerrada = _planilla.Cerrada;
			HayPlanilla = true;
		}

		Registro = null;
		CursanteAIncorporar = null;
	}

	/// <summary>
	/// Trae la letra de la división y su preceptor. No hay una consulta por identificador en
	/// <see cref="IServicioDivision"/>: se lista el curso y se busca la división del drill-down,
	/// que es exactamente lo que hace <c>GestionCatedrasViewModel</c> para resolver las letras.
	/// </summary>
	private async Task CargarDivisionAsync(Guid divisionID)
	{
		var divisiones = await _servicioDivision.ListarDivisionesAsync(new ListarDivisionesRequest(CursoID: _cursoStore.Curso.CursoID,
																								   CicloLectivo: _cicloLectivoStore.CicloLectivo));

		var division = divisiones.FirstOrDefault(d => d.DivisionID.Equals(divisionID));

		Division = division?.Descripcion ?? string.Empty;
		Preceptor = division?.Preceptor ?? string.Empty;
		TienePreceptor = division?.PreceptorID is not null;
	}
	#endregion

	#region AbrirPlanillaCommand
	/// <remarks>
	/// Exige <see cref="TienePreceptor"/>: sin preceptor, <c>AbrirPlanillaAsync</c> termina siempre
	/// en <c>SinDatosPreceptorException</c>, y dejar el botón habilitado sería hacerle descubrir esa
	/// regla al usuario a fuerza de un cartel de error. El aviso de la vista le dice qué hacer.
	/// </remarks>
	private bool CanExecuteAbrirPlanillaCommand()
	{
		return !HayPlanilla && TienePreceptor;
	}

	private async Task ExecuteAbrirPlanillaCommand()
	{
		try
		{
			if (_divisionStore.Division is null)
			{
				return;
			}

			await _servicioAsistencia.AbrirPlanillaAsync(new AbrirPlanillaRequest(DivisionID: _divisionStore.Division.DivisionID,
																				  Fecha: Fecha));

			await CargarPlanillaAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	/// <summary>
	/// Guarda común a los cuatro comandos de marcado: hacen falta una planilla abierta —el dominio
	/// rechaza marcar en una cerrada— y una fila sobre la que marcar.
	/// </summary>
	private bool PuedeMarcar(RegistroAsistenciaViewModel? registro)
	{
		return HayPlanilla && !Cerrada && registro is not null;
	}

	/// <remarks>
	/// La tardanza es la única marca que lleva un dato además del alumno, y el dominio rechaza una de
	/// cero minutos con <c>TardanzaSinMinutosException</c>.
	/// </remarks>
	private bool PuedeMarcarTardanza(RegistroAsistenciaViewModel? registro)
	{
		return PuedeMarcar(registro) && MinutosTardanza > 0;
	}

	/// <summary>
	/// Deja la fila elegida antes de marcarla, para que el panel de detalle hable del mismo alumno que
	/// se acaba de tocar.
	/// </summary>
	/// <remarks>
	/// Los botones de la fila no la seleccionan solos: el <c>Button</c> marca el clic como manejado y
	/// el <c>DataGrid</c> no llega a verlo. Sin esto, marcar desde la fila dejaría el panel de abajo
	/// mostrando al alumno anterior.
	/// </remarks>
	private void Elegir(RegistroAsistenciaViewModel registro)
	{
		Registro = registro;
	}

	/// <summary>La observación del formulario, o <c>null</c> si está en blanco: así la guarda el dominio.</summary>
	private string? ObservacionDelFormulario()
	{
		return string.IsNullOrWhiteSpace(Observacion) ? null : Observacion;
	}

	/// <summary>
	/// Cambiar de alumno vacía la observación del formulario.
	/// </summary>
	/// <remarks>
	/// Con los botones por fila, la nota tipeada para un alumno y nunca aplicada se quedaba en el campo
	/// y se la llevaba el siguiente que se marcara. La observación describe a UN registro, así que
	/// pertenece a la fila elegida y se va con ella.
	/// </remarks>
	partial void OnRegistroChanged(RegistroAsistenciaViewModel? value)
	{
		Observacion = string.Empty;
	}

	#region MarcarPresenteCommand
	private async Task ExecuteMarcarPresenteCommand(RegistroAsistenciaViewModel? registro)
	{
		try
		{
			if (_divisionStore.Division is null || registro is null)
			{
				return;
			}

			Elegir(registro);

			// MarcarPresenteAsync no lleva observación: estar presente es el caso sin nada que aclarar.
			await _servicioAsistencia.MarcarPresenteAsync(new MarcarPresenteRequest(DivisionID: _divisionStore.Division.DivisionID,
																					Fecha: Fecha,
																					CursanteID: registro.CursanteID));

			Observacion = string.Empty;
			await CargarPlanillaAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region MarcarAusenciaCommand
	private async Task ExecuteMarcarAusenciaCommand(RegistroAsistenciaViewModel? registro)
	{
		try
		{
			if (_divisionStore.Division is null || registro is null)
			{
				return;
			}

			Elegir(registro);

			await _servicioAsistencia.MarcarAusenciaAsync(new MarcarAusenciaRequest(DivisionID: _divisionStore.Division.DivisionID,
																					Fecha: Fecha,
																					CursanteID: registro.CursanteID,
																					Observacion: ObservacionDelFormulario()));

			Observacion = string.Empty;
			await CargarPlanillaAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region MarcarInasistenciaCommand
	private async Task ExecuteMarcarInasistenciaCommand(RegistroAsistenciaViewModel? registro)
	{
		try
		{
			if (_divisionStore.Division is null || registro is null)
			{
				return;
			}

			Elegir(registro);

			await _servicioAsistencia.MarcarInasistenciaAsync(new MarcarInasistenciaRequest(DivisionID: _divisionStore.Division.DivisionID,
																							Fecha: Fecha,
																							CursanteID: registro.CursanteID,
																							Observacion: ObservacionDelFormulario()));

			Observacion = string.Empty;
			await CargarPlanillaAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region MarcarTardanzaCommand
	private async Task ExecuteMarcarTardanzaCommand(RegistroAsistenciaViewModel? registro)
	{
		try
		{
			if (_divisionStore.Division is null || registro is null)
			{
				return;
			}

			Elegir(registro);

			await _servicioAsistencia.MarcarTardanzaAsync(new MarcarTardanzaRequest(DivisionID: _divisionStore.Division.DivisionID,
																					Fecha: Fecha,
																					CursanteID: registro.CursanteID,
																					Minutos: TimeSpan.FromMinutes(MinutosTardanza),
																					Observacion: ObservacionDelFormulario()));

			Observacion = string.Empty;
			await CargarPlanillaAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region IncorporarCursanteCommand
	private bool CanExecuteIncorporarCursanteCommand()
	{
		return HayPlanilla && !Cerrada && CursanteAIncorporar is not null;
	}

	private async Task ExecuteIncorporarCursanteCommand()
	{
		try
		{
			if (_divisionStore.Division is null || CursanteAIncorporar is null)
			{
				return;
			}

			await _servicioAsistencia.IncorporarCursanteAsync(new IncorporarCursanteRequest(DivisionID: _divisionStore.Division.DivisionID,
																							Fecha: Fecha,
																							CursanteID: CursanteAIncorporar.CursanteID));

			await CargarPlanillaAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region CerrarPlanillaCommand
	private bool CanExecuteCerrarPlanillaCommand()
	{
		return HayPlanilla && !Cerrada;
	}

	/// <summary>
	/// Confirma antes de cerrar porque cerrar deshabilita todo el marcado del día: a partir de ahí hay
	/// que reabrir la planilla para corregir cualquier registro.
	/// </summary>
	private async Task ExecuteCerrarPlanillaCommand()
	{
		try
		{
			if (_divisionStore.Division is null)
			{
				return;
			}

			if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
					titulo: $"Cerrar la planilla del { Fecha.ToString("D") }",
					mensaje: $"Quedan firmados { Presentes } presentes, { Ausencias } ausencias, { Inasistencias } inasistencias y { Tardanzas } tardanzas. "
						   + "Con la planilla cerrada no se puede marcar asistencia ni incorporar alumnos; se puede reabrir después.",
					verbo: "Cerrar planilla")))
			{
				await _servicioAsistencia.CerrarPlanillaAsync(new CerrarPlanillaRequest(DivisionID: _divisionStore.Division.DivisionID,
																						Fecha: Fecha));

				await CargarPlanillaAsync();
				_dialogService.MostrarInformacion($"Planilla del { Fecha.ToString("d") }: { Presentes } presentes y { Ausencias + Inasistencias } ausencias.", "Planilla cerrada");
			}
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region ReabrirPlanillaCommand
	private bool CanExecuteReabrirPlanillaCommand()
	{
		return HayPlanilla && Cerrada;
	}

	/// <remarks>
	/// Reabrir era la única operación de la pantalla que no preguntaba nada: un clic deshacía la
	/// firma del preceptor sin aviso. No borra registros, así que no va en rojo, pero sí cambia
	/// el estado de un documento que ya estaba cerrado.
	/// </remarks>
	private async Task ExecuteReabrirPlanillaCommand()
	{
		try
		{
			if (_divisionStore.Division is null)
			{
				return;
			}

			if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Revisar(
					titulo: $"Reabrir la planilla del { Fecha.ToString("D") }",
					mensaje: "La planilla vuelve a aceptar marcas y la firma del cierre deja de valer. "
						   + "Los registros ya cargados se conservan.",
					verbo: "Reabrir planilla")))
			{
				return;
			}

			await _servicioAsistencia.ReabrirPlanillaAsync(new ReabrirPlanillaRequest(DivisionID: _divisionStore.Division.DivisionID,
																					  Fecha: Fecha));

			await CargarPlanillaAsync();
			_dialogService.MostrarInformacion($"La planilla del { Fecha.ToString("d") } vuelve a aceptar marcas.", "Planilla reabierta");
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region EliminarPlanillaCommand
	/// <remarks>
	/// Pide sólo que haya planilla, sin mirar <see cref="Cerrada"/>. Una planilla cerrada no se puede
	/// eliminar, pero esa es una regla del dominio —<c>PlanillaCerradaNoEliminableException</c>— y no
	/// se duplica acá: el botón queda habilitado y el usuario recibe el mensaje del dominio por el
	/// <c>catch</c>, que es donde la regla se explica una sola vez.
	/// </remarks>
	private bool CanExecuteEliminarPlanillaCommand()
	{
		return HayPlanilla;
	}

	private async Task ExecuteEliminarPlanillaCommand()
	{
		try
		{
			if (_divisionStore.Division is null)
			{
				return;
			}

			if (_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
					titulo: $"Eliminar la planilla del { Fecha.ToString("D") }",
					consecuencia: $"Se borran los { Presentes + Ausencias + Inasistencias + Tardanzas } registros del día "
								+ "y las inasistencias dejan de contar para el legajo de los alumnos. "
								+ "Esta acción no se puede deshacer: la planilla hay que volver a abrirla y marcarla de cero.",
					verbo: "Eliminar planilla")))
			{
				await _servicioAsistencia.EliminarPlanillaAsync(new EliminarPlanillaRequest(DivisionID: _divisionStore.Division.DivisionID,
																							Fecha: Fecha));

				// Deja la pantalla en estado "sin planilla": es lo que corresponde después de eliminarla.
				await CargarPlanillaAsync();
				_dialogService.MostrarInformacion($"Planilla del { Fecha.ToString("d") }.", "Planilla eliminada");
			}
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region VerCommand
	/// <summary>Abre una de las tres vistas. Un parámetro vacío o desconocido vuelve a la planilla del día.</summary>
	private void ExecuteVerCommand(string? vista)
	{
		Vista = vista switch
		{
			VISTA_HISTORIAL => VISTA_HISTORIAL,
			VISTA_RESUMEN => VISTA_RESUMEN,
			_ => VISTA_PLANILLA
		};
	}
	#endregion

	#region Periodo
	/// <summary>Las dos consultas del período necesitan lo mismo: una división sobre la que preguntar.</summary>
	private bool CanExecuteConsultarPeriodoCommand()
	{
		return _divisionStore.Division is not null;
	}

	/// <summary>
	/// Mueve el período a los últimos N días, contando hoy, y vuelve a consultar la vista abierta.
	/// </summary>
	/// <remarks>
	/// Los atajos existen porque el período que se quiere mirar es casi siempre uno de tres, y
	/// elegirlo con dos calendarios son seis clics para algo que el usuario ya sabe decir en voz alta.
	/// Las fechas quedan a la vista y editables: el atajo las completa, no las reemplaza.
	/// </remarks>
	private void ExecuteVerUltimosDiasCommand(string? dias)
	{
		if (!int.TryParse(dias, out var cantidad) || cantidad <= 0)
		{
			return;
		}

		Hasta = DateTime.Today;
		Desde = DateTime.Today.AddDays(-(cantidad - 1));

		if (Vista == VISTA_HISTORIAL)
		{
			ListarPlanillasCommand.Execute(null);
		}
		else if (Vista == VISTA_RESUMEN)
		{
			ConsultarResumenCommand.Execute(null);
		}
	}
	#endregion

	#region ListarPlanillasCommand
	private async Task ExecuteListarPlanillasCommand()
	{
		try
		{
			if (_divisionStore.Division is null)
			{
				return;
			}

			var planillas = await _servicioAsistencia.ListarPlanillasAsync(new ListarPlanillasRequest(DivisionID: _divisionStore.Division.DivisionID,
																									  Desde: Desde,
																									  Hasta: Hasta));

			Planillas.Clear();

			foreach (var planilla in planillas)
			{
				Planillas.Add(new PlanillaResumenViewModel(planilla));
			}

			PlanillaElegida = null;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region ConsultarResumenCommand
	/// <remarks>
	/// Une dos fuentes, igual que la grilla del día: <c>ResumenCursanteResponse</c> trae identificadores
	/// y los nombres salen del listado de cursantes. Un alumno con registros en el período pero fuera
	/// del listado vigente —se dio de baja a mitad de mes— conserva su fila, porque sus faltas de ese
	/// período pasaron igual; lo que no se puede es nombrarlo, y la fila lo dice en lugar de aparecer
	/// en blanco.
	/// </remarks>
	private async Task ExecuteConsultarResumenCommand()
	{
		try
		{
			if (_divisionStore.Division is null)
			{
				return;
			}

			var divisionID = _divisionStore.Division.DivisionID;

			var cursantes = await _servicioCursante.ListarCursantesAsync(new BuscarListadoRequest(CursoID: _cursoStore.Curso.CursoID,
																								  DivisionID: divisionID,
																								  Periodo: _cicloLectivoStore.CicloLectivo));

			Dictionary<Guid, string> nombres = cursantes.ToDictionary(c => c.CursanteID, c => c.NombreCompleto);

			var resumen = await _servicioAsistencia.ConsultarResumenAsync(new ConsultarResumenRequest(DivisionID: divisionID,
																									  Desde: Desde,
																									  Hasta: Hasta));

			ResumenCursantes.Clear();

			foreach (var cursante in resumen.Cursantes)
			{
				var nombre = nombres.TryGetValue(cursante.CursanteID, out var encontrado)
					? encontrado
					: "Alumno sin inscripción vigente";

				ResumenCursantes.Add(new ResumenCursanteViewModel(cursante, nombre));
			}

			PlanillasDelPeriodo = resumen.Planillas;
			PlanillasAbiertas = resumen.PlanillasAbiertas;
			PresentesDelPeriodo = resumen.Presentes;
			AusenciasDelPeriodo = resumen.Ausencias;
			InasistenciasDelPeriodo = resumen.Inasistencias;
			TardanzasDelPeriodo = resumen.Tardanzas;
			AsistenciaDelPeriodo = $"{ resumen.PorcentajeAsistencia.ToString("0.#", CultureInfo.CurrentCulture) }%";

			// El período que informa el resumen es el que Core consultó, no el que tiene el formulario:
			// si los extremos venían al revés, Core los dio vuelta y la pantalla tiene que mostrar eso.
			Desde = resumen.Desde;
			Hasta = resumen.Hasta;
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region AbrirFechaCommand
	private bool CanExecuteAbrirFechaCommand()
	{
		return PlanillaElegida is not null;
	}

	/// <summary>
	/// Lleva la planilla elegida del historial a la vista del día.
	/// </summary>
	/// <remarks>
	/// Es lo que vuelve navegable al historial: sin esto sería una tabla de la que hay que copiar una
	/// fecha a mano en el calendario de la otra vista.
	/// </remarks>
	private async Task ExecuteAbrirFechaCommand()
	{
		try
		{
			if (PlanillaElegida is null)
			{
				return;
			}

			Fecha = PlanillaElegida.Fecha;
			Vista = VISTA_PLANILLA;

			await CargarPlanillaAsync();
		}
		catch (Exception ex)
		{
			_dialogService.MostrarError(ex.Message, TITULO);
		}
	}
	#endregion

	#region VolverCommand
	private bool CanExecuteVolverCommand()
	{
		return true;
	}

	private void ExecuteVolverCommand()
	{
		_gestionDivisionesNavigationService.Navigate();
	}
	#endregion
}
