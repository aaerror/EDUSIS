using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioMaterias;
using Core.ServicioMaterias.DTOs.Requests;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;

public class GestionSituacionRevistaViewModel : ViewModel, INotifyDataErrorInfo
{
    #region Services
    private readonly IServicioDocente _servicioDocente;
    private readonly IServicioMateria _servicioMateria;
    #endregion

    #region Store
    private readonly CursoStore _cursoStore = null;
    private readonly MateriaStore _materiaStore = null;
    #endregion

    #region ViewModels
    private MateriaViewModel _materia;
    private LegajoDocenteViewModel _legajoDocente;
    private SituacionRevistaViewModel _situacionRevistaINSERT;
    private SituacionRevistaViewModel _situacionRevistaUPDATE;
    #endregion

    private bool _habilitarDocenteEnFunciones;
    private bool _habilitarNuevaSituacionRevista;
    private bool _habilitarResultadoBuscar;
    private bool _habilitarUpdate;
    private bool _habilitarInsert;

    private string _buscarDocente;

    private ObservableCollection<LegajoDocenteViewModel> _docentes;
    private ObservableCollection<SituacionRevistaViewModel> _docentesEnMateria = new();

    #region Commands
    public ViewModelCommand BuscarCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand RegistrarCommand { get; }
    public ViewModelCommand CancelarCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    public ViewModelCommand ListarCommand { get; }
    public ViewModelCommand SeleccionarCommand { get; }
    #endregion

    private Dictionary<string, List<string>> _errorsByProperty = new();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public GestionSituacionRevistaViewModel(IServicioDocente servicioDocente, IServicioMateria servicioMateria, CursoStore cursoStore, MateriaStore materiaStore)
    {
        _servicioDocente = servicioDocente;
        _servicioMateria = servicioMateria;
        _cursoStore = cursoStore;
        _materiaStore = materiaStore;

        BuscarCommand = new ViewModelCommand(ExecuteBuscarCommand, CanExecuteBuscarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        RegistrarCommand = new ViewModelCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
        CancelarCommand = new ViewModelCommand(ExecuteCancelarCommand, CanExecuteCancelarCommand);
        ListarCommand = new ViewModelCommand(ExecuteListarCommand, CanExecuteListarCommand);
        SeleccionarCommand = new ViewModelCommand(ExecuteSeleccionarCommand, CanExecuteCommand);

        HabilitarUpdate = true;
        HabilitarInsert = false;
        HabilitarResultadoBuscar = false;
        ActualizarSituacionRevista();
    }

    public bool HabilitarDocenteEnFunciones => Materia.SituacionRevista is not null;

    public bool HabilitarNuevaSituacionRevista
    {
        get
        {
            return _habilitarNuevaSituacionRevista;
        }

        set
        {
            _habilitarNuevaSituacionRevista = value;
            OnPropertyChanged(nameof(HabilitarNuevaSituacionRevista));
        }
    }

    public bool HabilitarResultadoBuscar
    {
        get
        {
            return _habilitarResultadoBuscar;
        }

        set
        {
            _habilitarResultadoBuscar = value;
            OnPropertyChanged(nameof(HabilitarResultadoBuscar));
        }
    }

    public bool HabilitarUpdate
    {
        get
        {
            return _habilitarUpdate;
        }

        set
        {
            _habilitarUpdate = value;
            OnPropertyChanged(nameof(HabilitarUpdate));
        }
    }

    public bool HabilitarInsert
    {
        get
        {
            return _habilitarInsert;
        }

        set
        {
            _habilitarInsert = value;
            OnPropertyChanged(nameof(HabilitarInsert));
        }
    }

    private void ActualizarSituacionRevista()
    {
        try
        {
            var docentes = _servicioMateria.ListarCargosDocenteActivosSegunMateria(new ListarCargosDocenteActivoSegunMateriaRequest(CursoID: _cursoStore.Curso.CursoID,
                                                                                                                                    MateriaID: _materiaStore.Materia.MateriaID));
            if (docentes.Count == 0)
            {
                string messageBoxText = "No existen docentes registrados para esta materia.";
                string caption = "Docentes";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                DocentesEnMateria.Clear();
                foreach (var docente in docentes)
                {
                    DocentesEnMateria.Add(new SituacionRevistaViewModel(docente));
                }
            }
        }
        catch (Exception ex)
        {
            string messageBoxText = $"Error al cargar los docentes de la materia.\nError: {ex.Message}";
            MessageBox.Show(messageBoxText, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public MateriaViewModel Materia => _materiaStore.Materia;

    public LegajoDocenteViewModel LegajoDocente
    {
        get
        {
            return _legajoDocente;
        }

        set
        {
            _legajoDocente = value;
            OnPropertyChanged(nameof(LegajoDocente));
        }
    }

    public SituacionRevistaViewModel SituacionRevistaINSERT
    {
        get
        {
            return _situacionRevistaINSERT;
        }

        set
        {
            _situacionRevistaINSERT = value;
            OnPropertyChanged(nameof(SituacionRevistaINSERT));
        }
    }

    public SituacionRevistaViewModel SituacionRevistaUPDATE
    {
        get
        {
            return _situacionRevistaUPDATE;
        }

        set
        {
            _situacionRevistaUPDATE = value;
            OnPropertyChanged(nameof(SituacionRevistaUPDATE));
        }
    }

    public string BuscarDocente
    {
        get
        {
            return _buscarDocente;
        }

        set
        {
            _buscarDocente = value;
            OnPropertyChanged(nameof(BuscarDocente));
        }
    }

    public ObservableCollection<LegajoDocenteViewModel> Docentes
    {
        get
        {
            return _docentes;
        }

        set
        {
            _docentes = value;
        }
    }

    public ObservableCollection<SituacionRevistaViewModel> DocentesEnMateria
    {
        get
        {
            return _docentesEnMateria;
        }

        set
        {
            _docentesEnMateria = value;
        }
    }

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName);
    #endregion

    #region BuscarCommand
    private bool CanExecuteBuscarCommand(object obj) => !string.IsNullOrWhiteSpace(BuscarDocente);

    private void ExecuteBuscarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        try
        {
            var request = new BuscarDocentePorApellidoNombreRequest(BuscarDocente);
            var response = _servicioDocente.BuscarLegajoDocentePorApellidoNombre(request);

            if (response.IsNullOrEmpty())
            {
                messageBoxText = $"No existen coincidencias.";
                caption = "Buscar";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                HabilitarResultadoBuscar = false;

                return;
            }

            Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
            HabilitarResultadoBuscar = true;

            messageBoxText = $"Se encontraron { Docentes.Count } coincidencias.";
            caption = "Operación Exitosa";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                return SituacionRevistaUPDATE is not null && !SituacionRevistaUPDATE.FechaBaja.HasValue && !SituacionRevistaUPDATE.EnFunciones;
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
            case "Docente":
                messageBoxText = $"¿Está seguro que desea establecer el docente { SituacionRevistaUPDATE.Docente } a cargo de la materia { _materiaStore.Materia.Descripcion }?";
                caption = "Docente a Cargo de la Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        var request = new EstablecerDocenteDeAulaRequest(CursoID: _cursoStore.Curso.CursoID,
                                                                         MateriaID: _materiaStore.Materia.MateriaID,
                                                                         DocenteID: SituacionRevistaUPDATE.DocenteID);
                        _servicioMateria.EstablecerDocenteDeAula(request);
                        ActualizarSituacionRevista();

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                break;
        }
    }
    #endregion

    #region RegistrarCommand
    private bool CanExecuteRegistrarCommand(object obj) => HabilitarUpdate;

    private void ExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                HabilitarResultadoBuscar = false;
                HabilitarUpdate = false;
                HabilitarInsert = true;
                break;
        }
    }
    #endregion

    #region EliminarCommand
    private bool CanExecuteEliminarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                return SituacionRevistaUPDATE is not null && !SituacionRevistaUPDATE.FechaBaja.HasValue && SituacionRevistaUPDATE.EnFunciones;
            default:
                return false;
        }
    }

    private void ExecuteEliminarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Docente":
                messageBoxText = $"¿Está seguro que desea cambiar de situación de revista al docente { SituacionRevistaUPDATE.Docente } " +
                                 $"de la materia { _materiaStore.Materia.Descripcion }? Se va a eliminar al docente del cargo { SituacionRevistaUPDATE.CargoDescripcion } " +
                                 $"(Fecha Alta: { SituacionRevistaUPDATE.FechaAlta.ToString("D") })";
                caption = "Cambio de Situación de Revista";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        var request = new EliminarSituacionRevistaRequest(CursoID: _cursoStore.Curso.CursoID,
                                                                          MateriaID: _materiaStore.Materia.MateriaID,
                                                                          DocenteID: SituacionRevistaUPDATE.DocenteID);
                        _servicioMateria.QuitarDocenteDeMateria(request);
                        ActualizarSituacionRevista();

                        messageBoxText = $"Se quitó del cargo al docente correctamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                SituacionRevistaUPDATE = null;
                break;
        }
    }
    #endregion

    #region CancelarCommand
    private bool CanExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                return SituacionRevistaUPDATE is not null;
            default:
                return false;
        }
    }

    private void ExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                HabilitarResultadoBuscar = false;
                HabilitarUpdate = true;
                HabilitarInsert = false;

                //HistorialDataGrid.SelectedItem = null;
                break;
        }
        //Profesores.Concat(_materiaResponse.Profesores);
        //Horarios.Concat(_materiaResponse.Horarios);
    }
    #endregion

    #region ListarCommand
    private bool CanExecuteListarCommand(object obj) => HabilitarInsert;

    private void ExecuteListarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        try
        {
            var response = _servicioDocente.ListarDocentesActivos();
            if (response.IsNullOrEmpty())
            {
                messageBoxText = $"No existen docentes activos.";
                caption = "Listado docente";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                HabilitarResultadoBuscar = false;
                return;
            }

            Docentes = new ObservableCollection<LegajoDocenteViewModel>(response.Select(x => new LegajoDocenteViewModel(x)));
            HabilitarResultadoBuscar = true;

            messageBoxText = $"Se encontraron { Docentes.Count } docentes activos.";
            caption = "Operación Exitosa";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region SeleccionarCommand
    private bool CanExecuteCommand(object obj) => LegajoDocente is not null;

    private void ExecuteSeleccionarCommand(object obj)
    {
        HabilitarNuevaSituacionRevista = true;

        SituacionRevistaINSERT = new(null);
        SituacionRevistaINSERT.Docente = LegajoDocente.NombreCompleto;
        SituacionRevistaINSERT.DocenteID = LegajoDocente.DocenteID;
    }
    #endregion
}
