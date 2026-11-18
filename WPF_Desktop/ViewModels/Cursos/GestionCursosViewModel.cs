using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCursos;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Cursos.Curriculas;
using WPF_Desktop.ViewModels.Cursos.Divisiones;

namespace WPF_Desktop.ViewModels.Cursos;

internal partial class GestionCursosViewModel : ObservableObject
{
	#region Servicios
	private readonly IServicioCurso _servicioCursos;
	private readonly IDialogService _dialogService;
	private readonly INavigationService _registrarCursoNavigationService;
	private readonly INavigationService _gestionDivisionesNavigationService;
	private readonly INavigationService _registrarMateriaNavigationService;
	#endregion

	private CursoStore _cursoStore;
	private ObservableCollection<CursoViewModel> _cursos;


	// NotifyCanExecuteChangedFor y NO NotifyPropertyChangedFor: el atributo anterior levantaba
	// PropertyChanged para la PROPIEDAD del comando, que no es la señal que el botón escucha. El
	// binding releía la propiedad, obtenía la misma instancia de ICommand, la DP no cambiaba y
	// ButtonBase no volvía a evaluar nada. Los comandos del toolkit tampoco se cuelgan de
	// CommandManager.RequerySuggested, así que CanExecuteChanged no se levantaba NUNCA: el
	// ViewModel nacía con Curso en null, el botón Eliminar evaluaba CanExecute una sola vez y
	// quedaba gris para siempre. Eliminar un curso era inalcanzable desde la UI y todo el bloque
	// del diálogo de impacto de ExecuteEliminarCommandAsync era código muerto.
	//
	// No lo vio nadie porque compila sin warning —EliminarCommandAsync existe como propiedad, así
	// que el generador no tiene nada que objetar— y porque este archivo era el único del proyecto
	// con el atributo equivocado. Se encontró recorriendo la app a mano, no con una
	// suite. Lo fija GestionCursosViewModelTests y lo previene la guarda de convención
	// Ningun_NotifyPropertyChangedFor_apunta_a_un_comando.
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private CursoViewModel _curso;

	[ObservableProperty]
	private ListCollectionView _listCollection;

	// Mismo cambio que en Curso. Acá hoy no cambia ningún comportamiento, porque ni
	// CanExecuteEliminarCommandAsync ni el NavigationCommand vigente leen HabilitarCursos; se
	// corrige igual para que la intención quede escrita con el atributo que la cumple.
	[NotifyCanExecuteChangedFor(nameof(EliminarCommandAsync))]
	[NotifyCanExecuteChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private bool _habilitarCursos;

	[ObservableProperty]
	private bool _habilitarMessage = false;

	[ObservableProperty]
	private string _message = string.Empty;

	#region Commands
	public IAsyncRelayCommand CargarCursosCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionCursosViewModel(IServicioCurso servicioCursos,
								  IDialogService dialogService,
								  INavigationService<RegistrarCursosViewModel> registrarCursoNavigationService,
								  INavigationService<GestionDivisionesViewModel> gestionDivisionesNavigationService,
								  INavigationService<GestionCurriculasViewModel> registrarMateriaNavigationService,
								  CursoStore cursoStore)
	{
		_servicioCursos = servicioCursos;
		_dialogService = dialogService;
		_registrarCursoNavigationService = registrarCursoNavigationService;
		_gestionDivisionesNavigationService = gestionDivisionesNavigationService;
		_registrarMateriaNavigationService = registrarMateriaNavigationService;
		_cursoStore = cursoStore;

		_cursos = new ObservableCollection<CursoViewModel>();


		CargarCursosCommandAsync = new AsyncRelayCommand(ExecuteCargarCursosCommandAsync);
		EliminarCommandAsync = new AsyncRelayCommand<string>(ExecuteEliminarCommandAsync, CanExecuteEliminarCommandAsync);
		//NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand, CanExecuteNavigationCommand);
		NavigationCommand = new RelayCommand<string>(ExecuteNavigationCommand);

		HabilitarMessage = false;
	}

	#region CargarCursosCommandAsync
	private async Task ExecuteCargarCursosCommandAsync()
	{
		_cursos.Clear();
		var cursos = await _servicioCursos.ListarCursosAsync();
		if (cursos.Count is 0)
		{
			Message = "Todavía no hay cursos. Creá el primero con «Registrar curso».";
			HabilitarMessage = true;
			HabilitarCursos = false;
		}
		else
		{
			foreach (CursoResponse response in cursos)
			{
				_cursos.Add(new CursoViewModel(response));
			}

			ListCollection = new ListCollectionView(_cursos);
			ListCollection.GroupDescriptions.Add(new PropertyGroupDescription("NivelEducativo"));

			HabilitarMessage = false;
			HabilitarCursos = true;
		}
	}
	#endregion

	#region NavigationCommand
	/*private bool CanExecuteNavigationCommand(object obj)
	{
		switch (obj)
		{
			case "Curso":
				return true;
			case "Division":
				return HabilitarCursos;
			case "Materia":
				return HabilitarCursos;
			default:
				return true;
		}
	}*/

	private void ExecuteNavigationCommand(string? obj)
	{
		switch (obj)
		{
			case "Curso":
				_registrarCursoNavigationService.Navigate();
				break;
			case "Division":
				if (!ElegirCursoParaNavegar())
				{
					break;
				}

				_gestionDivisionesNavigationService.Navigate();
				break;
			case "Materia":
				if (!ElegirCursoParaNavegar())
				{
					break;
				}

				_registrarMateriaNavigationService.Navigate();
				break;
		}
	}

	/// <summary>
	/// Deja el curso elegido en el store y devuelve si se puede navegar al destino.
	/// </summary>
	/// <remarks>
	/// La guarda hace falta porque <c>CanExecuteNavigationCommand</c> está comentado —ver el
	/// constructor—, así que los dos botones quedan habilitados sin curso elegido. Antes esto
	/// escribía <c>null</c> en el store y la pantalla destino moría al construirse:
	/// <c>GestionDivisionesViewModel</c> lee <c>_cursoStore.Curso.Grado</c> en su constructor, y una
	/// excepción ahí sube por la factory de <c>NavigationService</c>, donde no hay nada que la
	/// atrape ni UI que la muestre.
	/// </remarks>
	private bool ElegirCursoParaNavegar()
	{
		if (Curso is null)
		{
			Message = "Debe seleccionar un curso del listado para continuar.";
			HabilitarMessage = true;

			return false;
		}

		_cursoStore.Curso = new CursoSeleccionado(Curso.CursoID, Curso.Grado, Curso.NivelEducativo);

		return true;
	}
	#endregion

	#region EliminarCommandAsync
	private bool CanExecuteEliminarCommandAsync(object? obj)
	{
		switch (obj)
		{
			case "Curso":
				return Curso is not null;
			default:
				return false;
		}
	}

	private async Task ExecuteEliminarCommandAsync(object? obj)
	{
		switch (obj)
		{
			case "Curso":
				string curso = $"{ Grados.Decir(Curso.Grado) } ({ Curso.NivelEducativo })";

				// El texto que había acá era falso en dos frentes desde el 2026-11-06: afirmaba que
				// "se eliminan también sus divisiones" —ahora el caso de uso las exige ya eliminadas—
				// y que "Core rechaza la operación si todavía tiene cursantes inscriptos", cuando lo
				// que rechaza son las divisiones. Las cifras salen del caso de uso.
				var requestCurso = new EliminarCursoRequest(Curso.CursoID);

				ImpactoEliminacionCursoResponse impacto;
				try
				{
					impacto = await _servicioCursos.ConsultarImpactoEliminacionCursoAsync(requestCurso);
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo averiguar qué se perdería");
					return;
				}

				// Con divisiones el borrado está condenado a fallar, así que no se pregunta: se dice
				// qué hay que hacer antes. Preguntar "¿seguro?" para después mostrar un error es
				// pedirle al usuario que confirme algo que no va a pasar.
				if (impacto.TieneDivisiones)
				{
					_dialogService.MostrarAdvertencia(
						$"El curso de { curso } tiene { impacto.Divisiones } "
						+ (impacto.Divisiones is 1 ? "división" : "divisiones")
						+ ". Hay que eliminarlas primero, una por una: cada división se lleva las "
						+ "inscripciones y las calificaciones de su matrícula.",
						"No se puede eliminar todavía");
					return;
				}

				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Eliminar el curso de { curso }",
						consecuencia: DescribirConsecuenciaDelCurso(impacto),
						verbo: "Eliminar curso")))
				{
					return;
				}

				try
				{
					await _servicioCursos.EliminarCurso(requestCurso);

					await ExecuteCargarCursosCommandAsync();
					_dialogService.MostrarInformacion($"{ curso } ya no figura en el listado.", "Curso eliminado");
				}
				catch (Exception ex)
				{
					_dialogService.MostrarError(ex.Message, "No se pudo eliminar el curso");
				}

				break;
		}
	}

	/// <summary>
	/// El texto de la consecuencia cuando el curso <strong>sí</strong> se puede eliminar, o sea
	/// cuando ya no le quedan divisiones. Sólo habla de currícula: sin divisiones no hay cursantes ni
	/// calificaciones que informar.
	/// </summary>
	/// <remarks>
	/// Nombra las cátedras porque cuelgan de la materia y no de la división: una materia puede
	/// conservar cátedras de divisiones ya eliminadas, y las limpia
	/// <c>MateriaEliminadaEventHandler</c> cuando el borrado despacha el evento.
	/// </remarks>
	internal static string DescribirConsecuenciaDelCurso(ImpactoEliminacionCursoResponse impacto)
	{
		if (!impacto.TieneCurricula)
		{
			return "El curso no tiene divisiones ni diseño curricular cargado. "
				 + "Esta acción no se puede deshacer.";
		}

		var partes = new List<string>();

		if (impacto.Curriculas > 0)
		{
			partes.Add(impacto.Curriculas is 1 ? "1 diseño curricular" : $"{ impacto.Curriculas } diseños curriculares");
		}

		if (impacto.Materias > 0)
		{
			partes.Add(impacto.Materias is 1 ? "1 materia" : $"{ impacto.Materias } materias");
		}

		if (impacto.Catedras > 0)
		{
			partes.Add(impacto.Catedras is 1 ? "1 cátedra" : $"{ impacto.Catedras } cátedras");
		}

		string lista = partes.Count is 1
			? partes[0]
			: $"{ string.Join(", ", partes.Take(partes.Count - 1)) } y { partes.Last() }";

		return $"Se eliminan { lista }, con los horarios cargados en ellas. "
			 + "Esta acción no se puede deshacer.";
	}
	#endregion
}