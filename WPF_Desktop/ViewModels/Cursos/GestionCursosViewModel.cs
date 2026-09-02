using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCursos;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Cursos;

internal partial class GestionCursosViewModel : ObservableObject
{
	#region Servicios
	private readonly IServicioCurso _servicioCursos;
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
								  INavigationService registrarCursoNavigationService,
								  INavigationService gestionDivisionesNavigationService,
								  INavigationService registrarMateriaNavigationService,
								  CursoStore cursoStore)
	{
		_servicioCursos = servicioCursos;
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
			Message = "No existen cursos agregados hasta el momento.";
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
				_cursoStore.Curso = Curso;
				_gestionDivisionesNavigationService.Navigate();
				break;
			case "Materia":
				_cursoStore.Curso = Curso;
				_registrarMateriaNavigationService.Navigate();
				break;
		}
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
		string messageBoxText = string.Empty;
		string caption = string.Empty;
		MessageBoxResult result;

		switch (obj)
		{
			case "Curso":
				messageBoxText = $"Se va a eliminar el curso:\n" +
								 $"Grado: { Curso.Grado }\n" +
								 $"Nivel Educativo: { Curso.NivelEducativo }\n\n" +
								 $"¿Desea continuar?";
				caption = "Eliminar Curso";

				result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result is MessageBoxResult.Yes)
				{
					try
					{
						await _servicioCursos.EliminarCurso(new EliminarCursoRequest(Curso.CursoID));
						MessageBox.Show("Curso eliminado correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
						
						ExecuteCargarCursosCommandAsync();
					}
					catch (Exception ex)
					{
						messageBoxText = $"Error al eliminar un nuevo curso. {ex.Message}";
						caption = "Error en la operación";
						MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
					}
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