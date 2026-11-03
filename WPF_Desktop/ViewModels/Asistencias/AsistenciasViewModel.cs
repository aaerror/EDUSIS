using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursos;
using Core.ServicioDivisiones;
using Core.ServicioDivisiones.DTOs.Requests;
using Core.ServicioDivisiones.DTOs.Responses;
using Core.ServicioCursos.DTOs.Responses;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Cursos.Divisiones;

namespace WPF_Desktop.ViewModels.Asistencias;

/// <summary>
/// Raíz de la sección «Asistencias»: elige curso y división, deja las dos en sus stores y entra a
/// la planilla.
/// </summary>
/// <remarks>
/// <para>
/// Existe porque <c>GestionAsistenciasViewModel</c> es una pantalla de drill-down: lee
/// <c>CursoStore</c>, <c>DivisionStore</c> y <c>CicloLectivoStore</c> dando por hecho que alguien
/// los escribió antes —de hecho desreferencia <c>_cursoStore.Curso.CursoID</c> sin chequear—, así
/// que colgarla directo de una entrada de menú la haría explotar en el constructor. Esta pantalla
/// es el tramo que faltaba del camino: hace lo mismo que hacían Cursos → Divisiones, pero sin
/// exigir los permisos de gestión que esas dos pantallas sí piden.
/// </para>
/// <para>
/// Lo que la justifica es el rol <c>Preceptoria</c>: sus tres permisos
/// (<c>TomarAsistencia</c>, <c>GenerarDocumentosAsistencia</c>, <c>GenerarReportesDeCursada</c>)
/// no habilitaban ninguna raíz del menú, así que entraba al sistema y se encontraba las cinco
/// secciones apagadas. Ver la matriz de <c>docs/ui.md</c> §3.
/// </para>
/// <para>
/// Las dos consultas que usa —<c>ListarCursosAsync</c> y <c>ListarDivisionesAsync</c>— no exigen
/// permiso, igual que el resto de las lecturas de <c>Core</c>.
/// </para>
/// </remarks>
internal partial class AsistenciasViewModel : ObservableObject
{
	#region Servicios
	private readonly IServicioCurso _servicioCurso;
	private readonly IServicioDivision _servicioDivision;
	private readonly IDialogService _dialogos;
	private readonly INavigationService _gestionAsistenciasNavigationService;
	#endregion

	#region Stores
	private readonly CursoStore _cursoStore;
	private readonly DivisionStore _divisionStore;
	private readonly CicloLectivoStore _cicloLectivoStore;
	#endregion

	public ObservableCollection<CursoViewModel> Cursos { get; }
	public ObservableCollection<DivisionViewModel> Divisiones { get; }

	/// <summary>Ciclo lectivo con el que se abre la planilla. Sale del store, que arranca en el año en curso.</summary>
	public string CicloLectivo => _cicloLectivoStore.CicloLectivo;

	[NotifyCanExecuteChangedFor(nameof(AbrirPlanillaCommand))]
	[ObservableProperty]
	private CursoViewModel _curso;

	[NotifyCanExecuteChangedFor(nameof(AbrirPlanillaCommand))]
	[ObservableProperty]
	private DivisionViewModel _division;

	/// <summary>
	/// Aviso al pie del formulario. No lo acompaña un <c>bool</c> de visibilidad: la vista usa
	/// el converter <c>visibleSiHayTexto</c>, así que el texto vacío ya es "no hay aviso" y no
	/// hay dos fuentes del mismo estado que puedan quedar desfasadas.
	/// </summary>
	[ObservableProperty]
	private string _mensaje = string.Empty;

	#region Commands
	public IAsyncRelayCommand CargarCursosCommandAsync { get; }
	public IRelayCommand AbrirPlanillaCommand { get; }
	#endregion


	public AsistenciasViewModel(IServicioCurso servicioCurso,
								IServicioDivision servicioDivision,
								IDialogService dialogos,
								INavigationService gestionAsistenciasNavigationService,
								CursoStore cursoStore,
								DivisionStore divisionStore,
								CicloLectivoStore cicloLectivoStore)
	{
		_servicioCurso = servicioCurso;
		_servicioDivision = servicioDivision;
		_dialogos = dialogos;
		_gestionAsistenciasNavigationService = gestionAsistenciasNavigationService;
		_cursoStore = cursoStore;
		_divisionStore = divisionStore;
		_cicloLectivoStore = cicloLectivoStore;

		Cursos = new ObservableCollection<CursoViewModel>();
		Divisiones = new ObservableCollection<DivisionViewModel>();

		CargarCursosCommandAsync = new AsyncRelayCommand(ExecuteCargarCursosCommandAsync);
		AbrirPlanillaCommand = new RelayCommand(ExecuteAbrirPlanillaCommand, CanExecuteAbrirPlanillaCommand);
	}

	#region CargarCursosCommandAsync
	private async Task ExecuteCargarCursosCommandAsync()
	{
		try
		{
			Cursos.Clear();
			Divisiones.Clear();

			foreach (CursoResponse response in await _servicioCurso.ListarCursosAsync())
			{
				Cursos.Add(new CursoViewModel(response));
			}

			Mensaje = Cursos.Count is 0
				? "Todavía no hay cursos cargados: sin curso no hay divisiones con las que tomar lista."
				: string.Empty;
		}
		catch (Exception ex)
		{
			_dialogos.MostrarError(ex.Message, "No se pudo cargar el listado de cursos");
		}
	}
	#endregion

	#region Divisiones del curso elegido
	/// <summary>
	/// Elegir un curso recarga sus divisiones. Es el equivalente al paso Cursos → Divisiones, pero
	/// sin salir de la pantalla: acá el curso no es un destino sino un filtro.
	/// </summary>
	partial void OnCursoChanged(CursoViewModel value)
	{
		Division = null;
		Divisiones.Clear();

		if (value is null)
		{
			return;
		}

		_ = CargarDivisionesAsync(value.CursoID);
	}

	private async Task CargarDivisionesAsync(Guid cursoID)
	{
		try
		{
			var divisiones = await _servicioDivision.ListarDivisionesAsync(
				new ListarDivisionesRequest(cursoID, _cicloLectivoStore.CicloLectivo));

			foreach (DivisionResponse response in divisiones)
			{
				Divisiones.Add(new DivisionViewModel(response));
			}

			Mensaje = Divisiones.Count is 0
				? $"El curso elegido no tiene divisiones en el ciclo { _cicloLectivoStore.CicloLectivo }."
				: string.Empty;
		}
		catch (Exception ex)
		{
			_dialogos.MostrarError(ex.Message, "No se pudieron cargar las divisiones del curso");
		}
	}
	#endregion

	#region AbrirPlanillaCommand
	private bool CanExecuteAbrirPlanillaCommand() =>
		Curso is not null && Division is not null;

	/// <summary>
	/// Escribe los tres stores del drill-down y navega. El orden importa: la pantalla destino los
	/// lee en su constructor, que corre dentro de <c>Navigate()</c>.
	/// </summary>
	private void ExecuteAbrirPlanillaCommand()
	{
		_cursoStore.Curso = new CursoSeleccionado(Curso.CursoID, Curso.Grado, Curso.NivelEducativo);
		_divisionStore.Division = new DivisionSeleccionada(Division.DivisionID);

		_gestionAsistenciasNavigationService.Navigate();
	}
	#endregion
}
