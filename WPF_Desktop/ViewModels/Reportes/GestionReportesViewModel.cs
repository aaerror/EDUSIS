using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursos;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioReportes;
using Core.ServicioReportes.DTOs.Requests;
using Core.Shared.Documentos;
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
/// El filtro por curso y división es opcional a propósito: sin él, el reporte abarca la
/// institución completa, que es el caso habitual. Las divisiones se cargan recién al elegir un
/// curso porque <c>IServicioDivision.ListarDivisionesAsync</c> pide curso y ciclo lectivo.
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

	[Required(AllowEmptyStrings = false, ErrorMessage = "Se debe ingresar el ciclo lectivo.")]
	[RegularExpression(@"^\d{4}$", ErrorMessage = "El ciclo lectivo son cuatro dígitos.", MatchTimeoutInMilliseconds = 2000)]
	[NotifyCanExecuteChangedFor(nameof(MatriculasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(AsistenciasCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(CalificacionesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(MateriasPendientesCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(TrayectoriasCommandAsync))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _cicloLectivo = string.Empty;

	[ObservableProperty]
	private ObservableCollection<CursoViewModel> _cursos = new();

	[ObservableProperty]
	private CursoViewModel? _curso;

	[ObservableProperty]
	private ObservableCollection<DivisionViewModel> _divisiones = new();

	[ObservableProperty]
	private DivisionViewModel? _division;

	[ObservableProperty]
	private DateTime _desde = new DateTime(DateTime.Today.Year, 3, 1);

	[ObservableProperty]
	private DateTime _hasta = DateTime.Today;

	[ObservableProperty]
	private string _mensaje = "El reporte abarca la institución completa salvo que elijas un curso y una división.";

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

		CargarCommandAsync = new AsyncRelayCommand(CargarCursosAsync);
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

		QuitarFiltroCommand = new RelayCommand(QuitarFiltro);

		CicloLectivo = _cicloLectivoStore.CicloLectivo;
	}

	#region Cambios de propiedad
	partial void OnCicloLectivoChanged(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			_cicloLectivoStore.CicloLectivo = value;
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
		if (Curso is null)
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
		Mensaje = "El reporte abarca la institución completa salvo que elijas un curso y una división.";
	}
	#endregion

	#region Emisión
	private bool CanExecuteEmitir() =>
		!string.IsNullOrWhiteSpace(CicloLectivo) && !GetErrors(nameof(CicloLectivo)).Any();

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
	}
	#endregion
}
