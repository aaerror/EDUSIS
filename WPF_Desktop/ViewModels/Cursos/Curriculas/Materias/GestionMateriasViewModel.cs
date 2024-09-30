using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioMaterias;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Docentes;
using Microsoft.IdentityModel.Tokens;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

public class GestionMateriasViewModel : ViewModel, INotifyDataErrorInfo
{
    #region Services
    private readonly INavigationService _gestionCursosNavigationService;
    private readonly INavigationService _gestionSituacionRevistaNavigationService;
    private readonly IServicioMateria _servicioMateria;
    private readonly IServicioDocente _servicioDocente;
    #endregion

    #region Store
    private readonly CursoStore _cursoStore;
    private readonly MateriaStore _materiaStore;
    #endregion

    #region Requests
    private RegistrarMateriaRequest _registrarMateriaRequest = null;
    private RegistrarDocenteEnMateriaRequest _registrarSituacionRevistaRequest = null;
    #endregion

    #region Responses
    private LegajoDocenteResponse _docenteInfoResponse = null;
    private SituacionRevistaResponse _situacionRevistaResponse = null;
    #endregion

    #region ViewModels
    private LegajoDocenteViewModel _docente;
    private MateriaViewModel _materia;
    #endregion

    public string Curso => $"{ _cursoStore.Curso.Grado }° Año de { _cursoStore.Curso.NivelEducativoDescripcion }".ToUpper();

    private string _descripcion = string.Empty;
    private int _cargaHoraria = 0;

    private bool _habilitarNotificacion = false;
    private bool _habilitarGestionMateria = false;
    private bool _habilitarRegistrarMateria = false;
    private bool _habilitarEditarMateria = false;

    public ObservableCollection<MateriaViewModel> _materias = new();

    #region Commands
    public ViewModelCommand NavigationCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand RegistrarCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    public ViewModelCommand CancelarCommand { get; }
    #endregion

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public GestionMateriasViewModel(INavigationService gestionCursosNavigationService,
                                    INavigationService gestionSituacionRevistaNavigationService,
                                    IServicioMateria servicioMateria,
                                    IServicioDocente servicioDocente,
                                    CursoStore cursoStore,
                                    MateriaStore materiaStore)
    {
        _gestionCursosNavigationService = gestionCursosNavigationService;
        _gestionSituacionRevistaNavigationService = gestionSituacionRevistaNavigationService;
        _servicioMateria = servicioMateria;
        _servicioDocente = servicioDocente;
        _cursoStore = cursoStore;
        _materiaStore = materiaStore;

        NavigationCommand = new ViewModelCommand(ExecuteNavigationCommand, CanExecuteNavigationCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        RegistrarCommand = new ViewModelCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
        CancelarCommand = new ViewModelCommand(ExecuteCancelarCommand, CanExecuteCancelarCommand);

        ActualizarMaterias();
    }

    private void ActualizarMaterias()
    {
        try
        {
            var request = new ListarMateriasSegunCursoRequest(CursoID: _cursoStore.Curso.CursoID);
            var response = _servicioMateria.ListarMateriasSegunCurso(request);
            if (response.IsNullOrEmpty())
            {
                /*
                string messageBoxText = "No existen materias adicionadas para este curso.";
                string caption = "Diseño de Curricula";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                */
                HabilitarNotificacion = true;
            }
            else
            {
                _materias.Clear();
                foreach (MateriaResponse materia in response)
                {
                    _materias.Add(new MateriaViewModel(_servicioMateria, materia, _materiaStore));
                }

                HabilitarNotificacion = false;
                HabilitarGestionMateria = true;
                HabilitarRegistrarMateria = false;
            }
        }
        catch (Exception ex)
        {
            string messageBoxText = $"Error al cargar las materias del curso.\nError: {ex.Message}";
            MessageBox.Show(messageBoxText, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public bool HabilitarNotificacion
    {
        get
        {
            return _habilitarNotificacion;
        }

        set
        {
            _habilitarNotificacion = value;
            OnPropertyChanged(nameof(HabilitarNotificacion));
        }
    }

    public bool HabilitarGestionMateria
    {
        get
        {
            return _habilitarGestionMateria;
        }

        set
        {
            _habilitarGestionMateria = value;
            OnPropertyChanged(nameof(HabilitarGestionMateria));
        }
    }

    public bool HabilitarRegistrarMateria
    {
        get
        {
            return _habilitarRegistrarMateria;
        }

        set
        {
            _habilitarRegistrarMateria = value;
            OnPropertyChanged(nameof(HabilitarRegistrarMateria));
        }
    }

    public bool HabilitarEditarMateria
    {
        get
        {
            return _habilitarEditarMateria;
        }

        set
        {
            _habilitarEditarMateria = value;
            OnPropertyChanged(nameof(HabilitarEditarMateria));
        }
    }

    public MateriaViewModel Materia
    {
        get
        {
            return _materia;
        }

        set
        {
            _materia = value;
            OnPropertyChanged(nameof(Materia));
            if (Materia is null)
            {
                HabilitarEditarMateria = false;
            }
        }
    }

    public ObservableCollection<MateriaViewModel> Materias
    {
        get
        {
            return _materias;
        }

        set
        {
            _materias = value;
            OnPropertyChanged(nameof(Materias));
        }
    }

    #region Properties RegistrarMateria
    public string Descripcion
    {
        get
        {
            return _descripcion;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Descripcion));
            _descripcion = value;
            OnPropertyChanged(nameof(Descripcion));

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                _errorsByProperty.Add(nameof(Descripcion), new List<string>
                {
                    "Se debe especificar el nombre de la materia."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Descripcion)));
            }
        }
    }

    public int CargaHoraria
    {
        get
        {
            return _cargaHoraria;
        }

        set
        {
            _cargaHoraria = value;
            OnPropertyChanged(nameof(CargaHoraria));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName);
    #endregion

    #region NavigationCommand
    private bool CanExecuteNavigationCommand(object obj)
    {
        switch (obj)
        {
            case "Cursos":
                return !HabilitarRegistrarMateria && !HabilitarEditarMateria;
            case "SituacionRevista":
                return Materia is not null && !HabilitarEditarMateria;
            default:
                return false;
        }
    }

    private void ExecuteNavigationCommand(object obj)
    {
        switch (obj)
        {
            case "Cursos":
                _gestionCursosNavigationService.Navigate();
                break;
            case "SituacionRevista":
                _materiaStore.Materia = Materia;
                _gestionSituacionRevistaNavigationService.Navigate();
                break;
        }
    }
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj)
    {
        switch (obj)
        {
            case "Insert":
                return !HasErrors;
            case "Update":
                return HabilitarEditarMateria;
            default:
                return false;
        }
    }

    private void ExecuteGuardarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Insert":
                messageBoxText = $"¿Está seguro que desea registrar la materia?\nMateria: { Descripcion }\nCarga horaria: { CargaHoraria + 1 } hora cátedra";
                caption = "Guardar Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        var request = new RegistrarMateriaRequest(CursoID: _cursoStore.Curso.CursoID,
                                                                  Descripcion: Descripcion,
                                                                  HorasCatedra: CargaHoraria);
                        _servicioMateria.RegistrarMateria(request);

                        messageBoxText = $"Materia registrada exitósamente.";
                        caption = "Operación Exitosa";

                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        HabilitarRegistrarMateria = false;
                        _descripcion = string.Empty;
                        _cargaHoraria = 0;

                        ActualizarMaterias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                        HabilitarRegistrarMateria = false;
                    }
                }

                break;
            case "Update":
                messageBoxText = $"¿Está seguro que desea guardar los cambios en la materia?";
                caption = "Guardar Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        var request = new ModificarMateriaRequest(CursoID: _cursoStore.Curso.CursoID,
                                                                 MateriaID: Materia.MateriaID,
                                                                 Descripcion: Materia.Descripcion,
                                                                 HorasCatedra: Materia.HorasCatedra);
                        _servicioMateria.ModificarMateria(request);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";

                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        HabilitarEditarMateria = false;
                        ActualizarMaterias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);

                        HabilitarEditarMateria = false;
                    }
                }
                break;
        }
    }
    #endregion

    #region RegistrarCommand
    private bool CanExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Update":
                return Materia is not null && !HabilitarEditarMateria;
            case "Insert":
                return !HabilitarRegistrarMateria && !HabilitarEditarMateria;
            default:
                return true;
        }
    }

    private async void ExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Update":
                HabilitarRegistrarMateria = false;
                HabilitarEditarMateria = true;
/*
                if (string.IsNullOrWhiteSpace(Descripcion))
                {
                    _errorsByProperty.Add(nameof(Descripcion), new List<string>()
                    {
                        "Se debe especificar el nombre de la materia."
                    });
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Descripcion)));

                    messageBoxText = $"Error al registrar una materia en el curso. Existen algunos campos vacíos y debe completarlos antes de poder continuar.";
                    caption = "Error al Registrar Materia";
                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                    break;
                }

                messageBoxText = $"¿Está seguro que desea agregar la materia a la currícula de { _cursoStore.Curso.Grado }° año de la { _cursoStore.Curso.NivelEducativoDescripcion }?\n\n" +
                                 $"Materia: { Descripcion }\nCarga horaria: { CargaHoraria + 1 } horas cátedra";
                caption = "Registrar Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _registrarMateriaRequest = new RegistrarMateriaRequest(CursoID: _cursoStore.Curso.CursoID,
                                                                               Descripcion: Descripcion,
                                                                               HorasCatedra: CargaHoraria + 1);
                        await _servicioMateria.RegistrarMateria(_registrarMateriaRequest);

                        messageBoxText = $"Nueva materia agregada a la currícula del curso.";
                        caption = "Operación Exitosa";

                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadMaterias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                        var materiaduplicada = ex.GetType();
                    }
                    finally
                    {
                        Descripcion = string.Empty;
                        CargaHoraria = 0;
                    }
                }
*/
                break;
            case "Insert":
                HabilitarNotificacion = false;
                HabilitarRegistrarMateria = true;
                HabilitarGestionMateria = false;
                break;
        }
    }
    #endregion

    #region EliminarCommand
    private bool CanExecuteEliminarCommand(object obj) =>
        Materia is not null && !HabilitarEditarMateria;

    private void ExecuteEliminarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        messageBoxText = $"¿Está seguro que desea eliminar la materia, { Materia.Descripcion }, del curso?";
        caption = "Eliminar Materia";
        result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (result is MessageBoxResult.Yes)
        {
            try
            {
                var request = new EliminarMateriaRequest(CursoID: _cursoStore.Curso.CursoID,
                                                         MateriaID: Materia.MateriaID);
                _servicioMateria.EliminarMateria(request);

                messageBoxText = $"La materia se eliminó correctamente de la currícula del curso.";
                caption = "Operación Exitosa";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                ActualizarMaterias();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region CancelarCommand
    private bool CanExecuteCancelarCommand(object obj)  
    {
        switch (obj)
        {
            case "Insert":
                return HabilitarRegistrarMateria;
            case "Update":
                return HabilitarEditarMateria;
            default:
                return false;
        }
    }

    private void ExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Insert":
                HabilitarRegistrarMateria = false;
                ActualizarMaterias();
                break;
            case "Update":
                HabilitarEditarMateria = false;
                ActualizarMaterias();
                break;
            default:
                break;
        }
    }
    #endregion
}