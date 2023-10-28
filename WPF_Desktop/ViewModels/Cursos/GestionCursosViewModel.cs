using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Responses;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Cursos;

public class GestionCursosViewModel : ViewModel
{
    private readonly IServicioCursos _servicioCursos;
    private readonly INavigationService _registrarCursoNavigationService;
    private readonly INavigationService _registrarMateriaNavigationService;
    private CursoStore _cursoStore;

    private CursoViewModel _curso = null;
    private ObservableCollection<CursoViewModel> _cursos = new ObservableCollection<CursoViewModel>();

    #region Commands
    public ViewModelCommand NuevoCursoCommand { get; }
    public ViewModelCommand NuevaDivisionCommand { get; }
    public ViewModelCommand NuevaMateriaCommand { get; }
    public ViewModelCommand MostrarCursosCommand { get; }
    #endregion


    public GestionCursosViewModel(IServicioCursos servicioCursos,
                                  INavigationService registrarCursoNavigationService,
                                  INavigationService registrarMateriaNavigationService,
                                  CursoStore cursoStore)
    {
        _servicioCursos = servicioCursos;
        _registrarCursoNavigationService = registrarCursoNavigationService;
        _registrarMateriaNavigationService = registrarMateriaNavigationService;
        _cursoStore = cursoStore;

        NuevoCursoCommand = new ViewModelCommand(command =>
        {
            _registrarCursoNavigationService.Navigate();
        });
        NuevaDivisionCommand = new ViewModelCommand(ExecuteNuevaDivisionCommand, CanExecuteNuevaDivisionCommand);
        NuevaMateriaCommand = new ViewModelCommand(ExecuteNuevaMateriaCommand, CanExecuteNuevaMateriaCommand);
        MostrarCursosCommand = new ViewModelCommand(ExecuteMostrarCursosCommand);
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

    public ObservableCollection<CursoViewModel> Cursos
    {
        get
        {
            return _cursos;
        }

        set
        {
            _cursos = value;
            OnPropertyChanged(nameof(Cursos));
        }
    }
    #endregion

    private void LoadCursos()
    {
        try
        {
            var lista = _servicioCursos.BuscarCursos();
            if (lista.Count == 0)
            {
                string messageBoxText = "No existen cursos agregados hasta el momento.";
                string caption = "Oferta Educativa";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                _cursos.Clear();
                foreach (CursoResponse response in lista)
                {
                    _cursos.Add(new CursoViewModel(response));
                }
            }
        }
        catch (Exception ex)
        {
            string messageBoxText = $"Error al cargar las materias del curso.\nError: {ex.InnerException.Message}";
            MessageBox.Show(messageBoxText, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #region MostrarCursosCommand
    private void ExecuteMostrarCursosCommand(object obj)
    {
        LoadCursos();
    }
    #endregion

    #region NuevaDivisionCommand
    private bool CanExecuteNuevaDivisionCommand(object obj) => Curso is not null;

    private void ExecuteNuevaDivisionCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        messageBoxText = $"Se va a registrar una nueva división al curso seleccionado:\n" +
                         $"Curso: { Curso.Descripcion }\n" +
                         $"Formación: { Curso.NivelEducativo }\n\n" +
                         $"¿Desea continuar?";
        caption = "Registrar división";

        result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _servicioCursos.AgregarDivisionAlCurso(Curso.CursoId);
                MessageBox.Show("Datos guardados correctamente",
                                "Operación exitosa",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                LoadCursos();
            }
            catch (Exception ex)
            {
                messageBoxText = $"Error al agregar una división al curso.\nError: {ex.Message}";
                caption = "Error";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region NuevaMateriaCommand
    private bool CanExecuteNuevaMateriaCommand(object obj) => Curso is not null;

    private void ExecuteNuevaMateriaCommand(object obj)
    {
        _cursoStore.Curso = Curso;
        _registrarMateriaNavigationService.Navigate();
    }
    #endregion
}
