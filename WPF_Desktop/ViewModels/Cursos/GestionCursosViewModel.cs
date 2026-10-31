using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCursos;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

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


	[NotifyPropertyChangedFor(nameof(EliminarCommandAsync))]
	[NotifyPropertyChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private CursoViewModel _curso;

	[ObservableProperty]
	private ListCollectionView _listCollection;

	[NotifyPropertyChangedFor(nameof(EliminarCommandAsync))]
	[NotifyPropertyChangedFor(nameof(NavigationCommand))]
	[ObservableProperty]
	private bool _habilitarCursos;

	[ObservableProperty]
	private bool _habilitarMessage = false;

	[ObservableProperty]
	private string _message = string.Empty;

	#region Commands
	public IAsyncRelayCommand CargarCursosCommandAsync { get; }
	public IAsyncRelayCommand EliminarCommandAsync { get; }
	public IRelayCommand ListarCommand { get; }
	public IRelayCommand NavigationCommand { get; }
	#endregion


	public GestionCursosViewModel(IServicioCurso servicioCursos,
								  IDialogService dialogService,
								  INavigationService registrarCursoNavigationService,
								  INavigationService gestionDivisionesNavigationService,
								  INavigationService registrarMateriaNavigationService,
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
		ListarCommand = new RelayCommand<string>(ExecuteListarCommand);

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

	private void ExecuteNavigationCommand(string obj)
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
	private bool CanExecuteEliminarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Curso":
				return Curso is not null;
			default:
				return false;
		}
	}

	private async Task ExecuteEliminarCommandAsync(object obj)
	{
		switch (obj)
		{
			case "Curso":
				string curso = $"{ Curso.Grado }° año ({ Curso.NivelEducativo })";

				if (!_dialogService.ConfirmarAccion(SolicitudDeDialogo.Destructiva(
						titulo: $"Eliminar el curso de { curso }",
						consecuencia: "Se eliminan también sus divisiones y su diseño curricular: las materias, "
									+ "las cátedras y los horarios cargados en este curso. "
									+ "Core rechaza la operación si todavía tiene cursantes inscriptos.",
						verbo: "Eliminar curso")))
				{
					return;
				}

				try
				{
					await _servicioCursos.EliminarCurso(new EliminarCursoRequest(Curso.CursoID));

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
	#endregion

	#region ListarCommand
	private void ExecuteListarCommand(object obj)
	{
		switch (obj)
		{
			case "Curso":
				ExecuteCargarCursosCommandAsync();
				break;
		}
	}
	#endregion
}