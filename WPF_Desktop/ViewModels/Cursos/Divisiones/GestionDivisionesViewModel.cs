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
    private readonly IServicioCursos _servicioCursos;
    private readonly IServicioDocentes _servicioDocentes;
    private readonly CursoStore _cursoStore;
    private readonly DivisionStore _divisionStore;

    private readonly INavigationService _gestionCursantesNavigationService;


    #region Request
    private EliminarDivisionRequest _elimniarDivisionRequest;
    #endregion

    private DivisionViewModel _division;
    private ObservableCollection<DivisionViewModel> _divisiones;

    #region Commands
    public ViewModelCommand ABMCommand { get; }
    public ViewModelCommand NavigationCommand { get; }
    #endregion


    public GestionDivisionesViewModel(IServicioCursos servicioCursos,
                                      IServicioDocentes servicioDocentes,
                                      INavigationService gestionCursantesNavigationService,
                                      CursoStore cursoStore,
                                      DivisionStore divisionStore)
    {
        _servicioCursos = servicioCursos;
        _servicioDocentes = servicioDocentes;
        _gestionCursantesNavigationService = gestionCursantesNavigationService;
        _cursoStore = cursoStore;
        _divisionStore = divisionStore;

        ActualizarDivisiones();

        ABMCommand = new ViewModelCommand(ExecuteABMCommand, CanExecuteABMCommand);
        NavigationCommand = new ViewModelCommand(ExecuteNavigationCommand, CanExecuteNavigationCommand);
    }

    private void ActualizarDivisiones()
    {
        if (CursoStore is not null)
        {
            var divisiones = _servicioCursos.BuscarDivisiones(_cursoStore.Curso.CursoID);
            Divisiones = new ObservableCollection<DivisionViewModel>(divisiones.Select(x => new DivisionViewModel(x)).ToList());
        }
    }

    #region Properties
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
            _divisiones = value;
            OnPropertyChanged(nameof(Divisiones));
        }
    }
    #endregion

    #region ABMCommand
    private bool CanExecuteABMCommand(object obj)
    {
        switch (obj)
        {
            case "Nueva":
                return CursoStore is not null;
            case "Eliminar":
                return Division is not null;
            default: return false;
        }
    }

    private void ExecuteABMCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Nueva":
                messageBoxText = $"Se va a registrar una nueva división en {CursoStore.Curso.Descripcion}° Año " +
                                 $"({CursoStore.Curso.NivelEducativo})\n\n" +
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
            case "Eliminar":
                messageBoxText = $"Se va a eliminar la división { Division.DivisionDescripcion } del curso.\n\n" +
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
