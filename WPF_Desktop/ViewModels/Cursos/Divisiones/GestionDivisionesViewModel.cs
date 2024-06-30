using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioDocentes;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

public class GestionDivisionesViewModel : ViewModel
{
    private readonly IServicioCurso _servicioCursos;
    private readonly IServicioDocente _servicioDocentes;
    private readonly CursoStore _cursoStore;
    private readonly DivisionStore _divisionStore;

    private readonly INavigationService _gestionCursantesNavigationService;

    #region Request
    private EliminarDivisionRequest _elimniarDivisionRequest;
    #endregion

    private int _totalDivisiones;
    private DivisionViewModel _division;
    private ObservableCollection<DivisionViewModel> _divisiones;

    public string Curso => _cursoStore.Curso.GradoDescripcion;
    public string CursoNivelEducativo => _cursoStore.Curso.NivelEducativoDescripcion;

    #region Commands
    public ViewModelCommand RegistrarCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    public ViewModelCommand NavigationCommand { get; }
    #endregion


    public GestionDivisionesViewModel(IServicioCurso servicioCursos,
                                      IServicioDocente servicioDocentes,
                                      INavigationService gestionCursantesNavigationService,
                                      CursoStore cursoStore,
                                      DivisionStore divisionStore)
    {
        _servicioCursos = servicioCursos;
        _servicioDocentes = servicioDocentes;
        _gestionCursantesNavigationService = gestionCursantesNavigationService;
        _cursoStore = cursoStore;
        _divisionStore = divisionStore;

        _divisiones = new ObservableCollection<DivisionViewModel>();
        ActualizarDivisiones();

        RegistrarCommand = new ViewModelCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
        NavigationCommand = new ViewModelCommand(ExecuteNavigationCommand, CanExecuteNavigationCommand);
    }

    private void ActualizarDivisiones()
    {
        if (CursoStore is not null)
        {
            var divisiones = _servicioCursos.BuscarDivisiones(_cursoStore.Curso.CursoID);
            Divisiones = new ObservableCollection<DivisionViewModel>(divisiones.Select(x => new DivisionViewModel(x)).ToList());
            TotalDivisiones = Divisiones.Count;
        }
    }

    #region Properties
    public int TotalDivisiones
    {
        get
        {
            return _totalDivisiones;
        }

        private set
        {
            _totalDivisiones = value;
            OnPropertyChanged(nameof(TotalDivisiones));
        }
    }

    public CursoStore CursoStore
    {
        get
        {
            return _cursoStore;
        }
    }

    public DivisionViewModel Division
    {
        get
        {
            return _division;
        }

        set
        {
            _division = value;
            OnPropertyChanged(nameof(Division));
        }
    }

    public ObservableCollection<DivisionViewModel> Divisiones
    {
        get
        {
            return _divisiones;
        }

        set
        {
            _divisiones.Clear();
            _divisiones = value;
            OnPropertyChanged(nameof(Divisiones));
        }
    }
    #endregion

    #region RegistrarCommand
    private bool CanExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Division":
                return CursoStore is not null;
            default: return false;
        }
    }

    private void ExecuteRegistrarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Division":
                messageBoxText = $"Se va a registrar una nueva división en { Curso }° Año\n\n" +
                                 $"¿Desea continuar?";
                caption = "Registrar División";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _servicioCursos.AgregarDivisionAlCurso(CursoStore.Curso.CursoID);
                        MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        ActualizarDivisiones();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                break;
        }
    }
    #endregion

    #region EliminarCommand
    private bool CanExecuteEliminarCommand(object obj)
    {
        switch (obj)
        {
            case "Division":
                return Division is not null;
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
            case "Division":
                messageBoxText = $"Se va a eliminar la división { Division.Descripcion } del curso.\n\n" +
                                 $"¿Desea continuar?";
                caption = "Eliminar División";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _elimniarDivisionRequest = new EliminarDivisionRequest(CursoStore.Curso.CursoID, Division.DivisionID);
                        _servicioCursos.QuitarDivisiosDelCurso(_elimniarDivisionRequest);
                        MessageBox.Show("División eliminada correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        ActualizarDivisiones();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                break;
        } 
    }
    #endregion

    #region NavigationCommand
    private bool CanExecuteNavigationCommand(object obj)
    {
        switch (obj)
        {
            case "Cursantes":
                return Division is not null;
            default: return false;
        }
    }

    private void ExecuteNavigationCommand(object obj)
    {
        switch (obj)
        {
            case "Cursantes":
                _divisionStore.Division = Division;
                _gestionCursantesNavigationService.Navigate();
                break;
        }
    }
    #endregion
}
