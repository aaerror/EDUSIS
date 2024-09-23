using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Cursos;

public class GestionCursosViewModel : ViewModel
{
    #region Servicios
    private readonly IServicioCurso _servicioCursos;
    private readonly INavigationService _registrarCursoNavigationService;
    private readonly INavigationService _gestionDivisionesNavigationService;
    private readonly INavigationService _registrarMateriaNavigationService;
    #endregion

    #region Commands
    public ViewModelCommand NavigateCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    public ViewModelCommand ListarCommand { get; }
    #endregion

    private CursoStore _cursoStore;

    private CursoViewModel _curso = null;

    private ObservableCollection<CursoViewModel> _cursos = new ObservableCollection<CursoViewModel>();
    private ListCollectionView _listCollectionView;


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

        NavigateCommand = new ViewModelCommand(ExecuteNavigateCommand, CanExecuteNavigateCommand);
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
        ListarCommand = new ViewModelCommand(ExecuteListarCommand);

        LoadCursos();
    }

    #region Properties
    public CursoViewModel Curso
    {
        get
        {
            return _curso;
        }

        set
        {
            _curso = value;
            OnPropertyChanged(nameof(Curso));
        }
    }

    public ListCollectionView ListCollectionView
    {
        get
        {
            return _listCollectionView;
        }

        set
        {
            _listCollectionView = value;
            OnPropertyChanged(nameof(ListCollectionView));
        }
    }
    #endregion

    private void LoadCursos()
    {
        try
        {
            _cursos.Clear();
            var lista = _servicioCursos.ListarCursos();
            if (lista.Count is 0)
            {
                string messageBoxText = "No existen cursos agregados hasta el momento.";
                string caption = "Oferta Educativa";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                foreach (CursoResponse response in lista)
                {
                    _cursos.Add(new CursoViewModel(response));
                }

                _listCollectionView = new ListCollectionView(_cursos);
                ListCollectionView.GroupDescriptions.Add(new PropertyGroupDescription("NivelEducativo"));
            }
        }
        catch (Exception ex)
        {
            string messageBoxText = $"Error al cargar las materias del curso.\nError: {ex.Message}";
            string caption = "Error en la operación";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #region NavigateCommand
    private bool CanExecuteNavigateCommand(object obj)
    {
        switch (obj)
        {
            case "Curso":
                return true;
            case "Division":
                return Curso is not null;
            case "Materia":
                return Curso is not null;
            default: return false;
        }
    }

    private void ExecuteNavigateCommand(object obj)
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

    #region EliminaCommand
    private bool CanExecuteEliminarCommand(object obj)
    {
        switch (obj)
        {
            case "Curso":
                return Curso is not null;
            default: return false;
        }
    }

    private void ExecuteEliminarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Curso":
                messageBoxText = $"Se va a eliminar el curso:\n" +
                                 $"Grado: {Curso.GradoDescripcion}\n" +
                                 $"Nivel Educativo: {Curso.NivelEducativoDescripcion}\n\n" +
                                 $"¿Desea continuar?";
                caption = "Eliminar Curso";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _servicioCursos.EliminarCurso(new EliminarCursoRequest(Curso.CursoID));
                        MessageBox.Show("Curso eliminado correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadCursos();
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
                LoadCursos();
                break;
        }
    }
    #endregion
}