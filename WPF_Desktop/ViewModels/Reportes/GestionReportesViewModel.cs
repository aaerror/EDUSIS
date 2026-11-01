using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursos;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioReportes;
using Core.ServicioReportes.DTOs.Requests;
using Core.Shared.Documentos;
using Domain.Cursos;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using WPF_Desktop.ViewModels.Cursos;

namespace WPF_Desktop.ViewModels.Reportes;

/// <summary>
/// Emisión de los reportes institucionales: se fija el ciclo lectivo, opcionalmente se acota a
/// una división, y se emite el reporte.
/// </summary>
/// <remarks>
/// <para>
/// El ciclo lectivo se <b>elige</b>, no se tipea: la lista sale del catálogo <c>ciclo_lectivo</c>
/// por <c>IServicioReporte.ListarCiclosLectivosAsync</c>. Antes era un campo libre de cuatro
/// dígitos y se podía pedir un reporte de un año que la base no habilita, que recién fallaba al
/// emitir.
/// </para>
/// <para>
/// El filtro por curso y división es opcional a propósito: sin él, el reporte abarca la
/// institución completa, que es el caso habitual. Las divisiones se cargan recién al elegir un
/// curso porque <c>IServicioDivision.ListarDivisionesAsync</c> pide curso y ciclo lectivo.
/// </para>
/// </remarks>
internal partial class GestionReportesViewModel : ObservableValidator
{
	#region Servicios
	private readonly IServicioReporte _servicioReporte;
	private readonly IServicioCurso _servicioCurso;
	private readonly IServicioDivision _servicioDivision;
	private readonly IArchivoService _archivos;
	private readonly IDialogService _dialogos;
	#endregion

	#region Stores
	private readonly CicloLectivoStore _cicloLectivoStore;
	#endregion

	private const string SIN_FILTRO = "Elegí el ciclo lectivo y emití el reporte que necesites. Sin curso y división, abarca la institución completa.";

	[ObservableProperty]
	private ObservableCollection<string> _ciclosLectivos = new();

	// Sigue siendo string y no int: el período es un string en el dominio (CicloLectivo.Periodo) y
	// así viaja al request sin conversión. La validación de formato que había acá —cuatro dígitos—
	// se fue con el campo libre: ahora el valor sale del catálogo de la base y lo único que puede
	// faltar es la elección.
	[Required(AllowEmptyStrings = false, ErrorMessage = "Se debe elegir el ciclo lectivo.")]
	[NotifyCanExecuteChangedFor(nameof(MatriculasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(AsistenciasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CalificacionesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasPendientesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(TrayectoriasCommandAsync))]
	[NotifyPropertyChangedFor(nameof(Alcance))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _cicloLectivo = string.Empty;

	[ObservableProperty]
	private ObservableCollection<CursoViewModel> _cursos = new();

	[NotifyCanExecuteChangedFor(nameof(QuitarFiltroCommand))]
	[NotifyPropertyChangedFor(nameof(Alcance))]
	[NotifyPropertyChangedFor(nameof(CursoSinDivision))]
	[ObservableProperty]
	private CursoViewModel? _curso;

	[ObservableProperty]
	private ObservableCollection<DivisionViewModel> _divisiones = new();

	[NotifyCanExecuteChangedFor(nameof(QuitarFiltroCommand))]
	[NotifyPropertyChangedFor(nameof(Alcance))]
	[NotifyPropertyChangedFor(nameof(CursoSinDivision))]
	[ObservableProperty]
	private DivisionViewModel? _division;

	[ObservableProperty]
	private DateTime _desde = new DateTime(DateTime.Today.Year, 3, 1);

	[ObservableProperty]
	private DateTime _hasta = DateTime.Today;

	/// <summary>
	/// Hay una emisión en curso. Apaga los cinco botones mientras tanto: generar el reporte de una
	/// institución entera no es instantáneo, y sin esto la pantalla no da señal de estar haciendo
	/// algo y se termina apretando dos veces.
	/// </summary>
	[NotifyCanExecuteChangedFor(nameof(MatriculasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(AsistenciasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CalificacionesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasPendientesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(TrayectoriasCommandAsync))]
	[ObservableProperty]
	private bool _emitiendo;

	[ObservableProperty]
	private string _mensaje = SIN_FILTRO;

	/// <summary>
	/// Qué va a abarcar el PDF, dicho con las mismas palabras que lleva impresas el documento. Es
	/// la única forma de ver el alcance antes de emitir: el request sólo lleva período y división.
	/// </summary>
	public string Alcance
	{
		get
		{
			if (string.IsNullOrWhiteSpace(CicloLectivo))
			{
				return "Falta elegir el ciclo lectivo";
			}

			return Division is not null && Curso is not null
				? $"Ciclo { CicloLectivo } · { Describir(Curso) } · División { Division.Descripcion }"
				: $"Ciclo { CicloLectivo } · Institución completa";
		}
	}

	/// <summary>
	/// Hay curso elegido pero no división. Importa decirlo: <c>ReportePorCicloRequest</c> sólo
	/// lleva la división, así que un curso sin división no acota nada y el reporte sale igual de
	/// completo que sin filtro.
	/// </summary>
	public bool CursoSinDivision => Curso is not null && Division is null;

	public bool HayFiltro => Curso is not null || Division is not null;

	#region AsyncCommands
	public IAsyncRelayCommand CargarCommandAsync { get; }
	public IAsyncRelayCommand CargarDivisionesCommandAsync { get; }
	public IAsyncRelayCommand MatriculasCommandAsync { get; }
	public IAsyncRelayCommand AsistenciasCommandAsync { get; }
	public IAsyncRelayCommand CalificacionesCommandAsync { get; }
	public IAsyncRelayCommand MateriasPendientesCommandAsync { get; }
	public IAsyncRelayCommand TrayectoriasCommandAsync { get; }
	#endregion

	#region Commands
	public IRelayCommand QuitarFiltroCommand { get; }
	#endregion


	public GestionReportesViewModel(IServicioReporte servicioReporte,
									IServicioCurso servicioCurso,
									IServicioDivision servicioDivision,
									IArchivoService archivos,
									IDialogService dialogos,
									CicloLectivoStore cicloLectivoStore)
	{
		_servicioReporte = servicioReporte;
		_servicioCurso = servicioCurso;
		_servicioDivision = servicioDivision;
		_archivos = archivos;
		_dialogos = dialogos;
		_cicloLectivoStore = cicloLectivoStore;

		CargarCommandAsync = new AsyncRelayCommand(CargarAsync);
		CargarDivisionesCommandAsync = new AsyncRelayCommand(CargarDivisionesAsync);

		MatriculasCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioReporte.GenerarReporteMatriculasAsync(request)), CanExecuteEmitir);
		CalificacionesCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioReporte.GenerarReporteCalificacionesAsync(request)), CanExecuteEmitir);
		MateriasPendientesCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioReporte.GenerarReporteMateriasPendientesAsync(request)), CanExecuteEmitir);
		TrayectoriasCommandAsync = new AsyncRelayCommand(
			() => EmitirAsync(request => _servicioReporte.GenerarReporteTrayectoriasAsync(request)), CanExecuteEmitir);

		AsistenciasCommandAsync = new AsyncRelayCommand(EmitirAsistenciasAsync, CanExecuteEmitir);

		QuitarFiltroCommand = new RelayCommand(QuitarFiltro, () => HayFiltro);

		// El ciclo lectivo NO se asigna acá: el valor del store se elige recién cuando llegó el
		// catálogo, en CargarCiclosLectivosAsync. Un SelectedItem que no está en ItemsSource lo
		// descarta el propio ComboBox, así que asignarlo antes de tener la lista lo perdería.
	}

	#region Cambios de propiedad
	partial void OnCicloLectivoChanged(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			_cicloLectivoStore.CicloLectivo = value;
		}

		// Las divisiones se listan por curso Y ciclo lectivo: cambiar de año sin rearmar la lista
		// dejaría elegida una división de otro período.
		if (Curso is not null)
		{
			Division = null;
			CargarDivisionesCommandAsync.Execute(null);
		}
	}

	partial void OnCursoChanged(CursoViewModel? value)
	{
		// Las divisiones de un curso dependen también del ciclo lectivo, así que la lista se
		// rearma cada vez que cambia el curso en lugar de cargarse una sola vez.
		Division = null;
		Divisiones = new ObservableCollection<DivisionViewModel>();

		if (value is not null)
		{
			CargarDivisionesCommandAsync.Execute(null);
		}
	}
	#endregion

	#region Carga
	private async Task CargarAsync()
	{
		await CargarCiclosLectivosAsync();
		await CargarCursosAsync();
	}

	private async Task CargarCiclosLectivosAsync()
	{
		try
		{
			var ciclos = await _servicioReporte.ListarCiclosLectivosAsync();

			CiclosLectivos = new ObservableCollection<string>(ciclos);
			CicloLectivo = Preferir(ciclos);

			if (CiclosLectivos.Count == 0)
			{
				Mensaje = "La base no tiene ningún ciclo lectivo habilitado, así que no hay período contra el cual emitir.";
			}
		}
		catch (Exception ex)
		{
			_dialogos.MostrarError(ex.Message, "Error al cargar los ciclos lectivos");
		}
	}

	/// <summary>
	/// Qué período queda elegido al abrir: el que venía de otra pantalla, si no el año en curso, y
	/// si ninguno de los dos está habilitado, el más reciente del catálogo. El store arranca en el
	/// año en curso y nadie garantiza que la base lo tenga cargado.
	/// </summary>
	private string Preferir(IReadOnlyCollection<string> ciclos)
	{
		if (ciclos.Contains(_cicloLectivoStore.CicloLectivo))
		{
			return _cicloLectivoStore.CicloLectivo;
		}

		var añoEnCurso = DateTime.Today.Year.ToString();

		return ciclos.Contains(añoEnCurso)
			? añoEnCurso
			: ciclos.FirstOrDefault() ?? string.Empty;
	}

	private async Task CargarCursosAsync()
	{
		try
		{
			var cursos = await _servicioCurso.ListarCursosAsync();
			Cursos = new ObservableCollection<CursoViewModel>(cursos.Select(x => new CursoViewModel(x)));
		}
		catch (Exception ex)
		{
			_dialogos.MostrarError(ex.Message, "Error al cargar los cursos");
		}
	}

	private async Task CargarDivisionesAsync()
	{
		if (Curso is null || string.IsNullOrWhiteSpace(CicloLectivo))
		{
			return;
		}

		try
		{
			var divisiones = await _servicioDivision.ListarDivisionesAsync(new ListarDivisionesRequest(Curso.CursoID, CicloLectivo));
			Divisiones = new ObservableCollection<DivisionViewModel>(divisiones.Select(x => new DivisionViewModel(x)));
		}
		catch (Exception ex)
		{
			_dialogos.MostrarError(ex.Message, "Error al cargar las divisiones");
		}
	}

	private void QuitarFiltro()
	{
		Curso = null;
		Division = null;
		Mensaje = SIN_FILTRO;
	}
	#endregion

	#region Emisión
	private bool CanExecuteEmitir() =>
		!Emitiendo && !string.IsNullOrWhiteSpace(CicloLectivo) && !GetErrors(nameof(CicloLectivo)).Any();

	private async Task EmitirAsync(Func<ReportePorCicloRequest, Task<DocumentoGenerado>> emitir)
	{
		var request = new ReportePorCicloRequest(CicloLectivo, Division?.DivisionID);

		await EntregarAsync(() => emitir(request));
	}

	private async Task EmitirAsistenciasAsync()
	{
		if (Desde.Date > Hasta.Date)
		{
			_dialogos.MostrarAdvertencia("El inicio del período es posterior a su fin.", "Período inválido");
			return;
		}

		var request = new ReporteAsistenciasRequest(CicloLectivo, Desde, Hasta, Division?.DivisionID);

		await EntregarAsync(() => _servicioReporte.GenerarReporteAsistenciasAsync(request));
	}

	private async Task EntregarAsync(Func<Task<DocumentoGenerado>> generar)
	{
		try
		{
			Emitiendo = true;

			var documento = await generar();
			var ruta = _archivos.GuardarYAbrir(documento);

			if (ruta is not null)
			{
				Mensaje = $"Reporte guardado en { ruta }";
			}
		}
		catch (Exception ex)
		{
			_dialogos.MostrarError(ex.Message, "Error al emitir el reporte");
		}
		finally
		{
			Emitiendo = false;
		}
	}
	#endregion

	/// <summary>Curso en la misma forma en que lo imprime el encabezado del reporte.</summary>
	private static string Describir(CursoViewModel curso)
	{
		var grado = Grados.Decir(curso.Grado);

		return string.IsNullOrEmpty(grado) ? curso.NivelEducativo : $"{ grado } { curso.NivelEducativo }";
	}
}
