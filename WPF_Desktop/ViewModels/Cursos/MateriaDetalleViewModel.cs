using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioDocentes.DTOs.Responses;
using Domain.Cursos.Materias;
using Domain.Docentes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class MateriaDetalleViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioCursos _servicioCursos;

    #region Request
    private EliminarSituacionRevistaRequest _eliminarSituacionRevista = null;
    private CrearDocenteEnFuncionesRequest _crearDocenteEnFunciones = null;
    #endregion

    #region Response
    private readonly MateriaResponse _materiaResponse = null;
    #endregion

    private bool _editarMateria = false;

    private Guid _materia = Guid.Empty;
    private string _descripcion = string.Empty;
    private int _horasCatedra;
    private Guid? _docenteID = Guid.Empty;
    private string _nombreCompletoProfesor = string.Empty;
    private SituacionRevistaViewModel _situacionRevistaProfesor;
    private ObservableCollection<SituacionRevistaViewModel> _profesores = new();
    private ObservableCollection<HorarioViewModel> _horarios = new();

    private Dictionary<string, List<string>> _errorsByProperty = new();
    
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand CancelarCommand { get; }
    public ViewModelCommand EditarCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand HorarioCommand { get; }
    #endregion


    public MateriaDetalleViewModel(IServicioCursos servicioCursos, MateriaResponse materiaResponse)
    {
        _servicioCursos = servicioCursos;
        _profesores.Clear();
        _horarios.Clear();

        if (materiaResponse is not null)
        {
            _materiaResponse = materiaResponse;

            Materia = _materiaResponse.Materia;
            Descripcion = _materiaResponse.Descripcion;
            HorasCatedra = _materiaResponse.HorasCatedra;
            DocenteID = _materiaResponse.Profesor;
            NombreCompletoProfesor = _materiaResponse.NombreCompletoProfesor;

            if (_materiaResponse.Profesores.Count > 0)
            {
                foreach (SituacionRevistaResponse response in _materiaResponse.Profesores)
                {
                    Profesores.Add(new SituacionRevistaViewModel(response));
                }
            }

            if (_materiaResponse.Horarios.Count > 0)
            {
                foreach (HorarioResponse response in _materiaResponse.Horarios)
                {
                    Horarios.Add(new HorarioViewModel(response));
                }
            }
        }

        EditarMateria = false;

        CancelarCommand = new ViewModelCommand(ExecuteCancelarCommand, CanExecuteCancelarCommand);
        EditarCommand = new ViewModelCommand(ExecuteEditarCommand, CanExecuteEditarCommand);
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        HorarioCommand = new ViewModelCommand(ExecuteHorarioCommand, CanExecuteHorarioCommand);
    }

    public void AgregarDocenteEnFunciones(Guid docenteID)
    {
        var situacionRevista = _profesores.Where(x => x.DocenteID.Equals(docenteID) && x.FechaBaja is null)
                                          .FirstOrDefault();
        situacionRevista.EnFunciones = true;
        DocenteID = situacionRevista.DocenteID;
        NombreCompletoProfesor = situacionRevista.NombreCompleto;
    }

    public void QuitarDocenteEnFunciones(SituacionRevistaViewModel situacionRevista)
    {
        if (situacionRevista is not null)
        {
            situacionRevista.EnFunciones = false;

            DocenteID = Guid.Empty;
            NombreCompletoProfesor = string.Empty;
        }
    }

    #region Properties
    public bool EditarMateria
    {
        get
        {
            return _editarMateria;
        }

        set
        {
            _editarMateria = value;
            OnPropertyChanged(nameof(EditarMateria));
        }
    }

    public Guid Materia
    {
        get
        {
            return _materia;
        }
        set
        {
            _materia = value;
            OnPropertyChanged(nameof(Materia));
        }
    }

    public string Descripcion
    {
        get
        {
            return _descripcion;
        }
        set
        {
            _descripcion = value;
            OnPropertyChanged(nameof(Descripcion));
            _errorsByProperty.Remove(nameof(Descripcion));

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                _errorsByProperty.Add(nameof(Descripcion), new List<string>
                {
                    "Se debe especificar el nombre de la materia."
                });
            }
        }
    }

    public int HorasCatedra
    {
        get
        {
            return _horasCatedra;
        }

        set
        {
            _errorsByProperty.Remove(nameof(HorasCatedra));
            _horasCatedra = value;
            OnPropertyChanged(nameof(HorasCatedra));

            if (HorasCatedra <= 0)
            {
                _errorsByProperty.Add(nameof(HorasCatedra), new List<string>
                {
                    "La materia debe tener al menos una hora cátedra."
                });
            }
        }
    }

    public int HorasCatedraSinAsignar
    {
        get
        {
            return HorasCatedra - _horarios.Count();
        }
    }

    public Guid? DocenteID
    {
        get
        {
            return _docenteID;
        }
        set
        {
            _docenteID = value;
            OnPropertyChanged(nameof(DocenteID));
        }
    }

    public string NombreCompletoProfesor
    {
        get
        {
            return _nombreCompletoProfesor;
        }

        set
        {
            _nombreCompletoProfesor = value;
            OnPropertyChanged(nameof(NombreCompletoProfesor));
        }
    }

    public int CantidadProfesores
    {
        get
        {
            return _profesores.Count();
        }
    }

    public SituacionRevistaViewModel SituacionRevistaProfesor
    {
        get
        {
            return _situacionRevistaProfesor;
        }

        set
        {
            _situacionRevistaProfesor = value;
            OnPropertyChanged(nameof(SituacionRevistaProfesor));
        }
    }

    public ObservableCollection<SituacionRevistaViewModel> Profesores
    {
        get
        {
            return _profesores;
        }

        set
        {
            _profesores = value;
            OnPropertyChanged(nameof(Profesores));
        }
    }

    public ObservableCollection<HorarioViewModel> Horarios
    {
        get
        {
            return _horarios;
        }

        set
        {
            _horarios = value;
            OnPropertyChanged(nameof(Horarios));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName);
    #endregion

    #region CancelarCommand
    private bool CanExecuteCancelarCommand(object obj) => EditarMateria;

    private void ExecuteCancelarCommand(object obj)
    {
        EditarMateria = false;

        Descripcion = _materiaResponse.Descripcion;
        Materia = _materiaResponse.Materia;
        Descripcion = _materiaResponse.Descripcion;
        HorasCatedra = _materiaResponse.HorasCatedra;
        DocenteID = _materiaResponse.Profesor;
        //Profesores.Concat(_materiaResponse.Profesores);
        //Horarios.Concat(_materiaResponse.Horarios);

    }
    #endregion

    #region EditarCommand
    private bool CanExecuteEditarCommand(object obj) => !EditarMateria;

    private void ExecuteEditarCommand(object obj)
    {
        EditarMateria = true;
    }
    #endregion

    #region EliminarCommand
    private bool CanExecuteEliminarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                return SituacionRevistaProfesor is not null && SituacionRevistaProfesor.FechaBaja is null;
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
                messageBoxText = $"¿Está seguro que desea cambiar de situación de revista al docente { SituacionRevistaProfesor.NombreCompleto } " +
                                 $"de la materia { Descripcion }? Se va a eliminar al docente del cargo { SituacionRevistaProfesor.CargoDescripcion } " +
                                 $"(Fecha Alta: { SituacionRevistaProfesor.FechaAlta.ToString("D") })";
                caption = "Cambio de Situación de Revista";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _eliminarSituacionRevista = new EliminarSituacionRevistaRequest(_materiaResponse.Curso,
                                                                                        _materiaResponse.Materia,
                                                                                        SituacionRevistaProfesor.DocenteID,
                                                                                        DateTime.Now);
                        _servicioCursos.QuitarDocenteDeMateria(_eliminarSituacionRevista);

                        messageBoxText = $"Se quitó del cargo al docente correctamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        SituacionRevistaProfesor.FechaBaja = DateTime.Now;
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                SituacionRevistaProfesor = null;
                break;

            case "Horario":

                break;
        }
    }
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj)
    {
        switch (obj)
        {
            case "DocenteEnFunciones":
                return SituacionRevistaProfesor is not null && SituacionRevistaProfesor.FechaBaja is null && !SituacionRevistaProfesor.EnFunciones;
            default:
                return EditarMateria;
        }
    }

    private void ExecuteGuardarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "DocenteEnFunciones":
                messageBoxText = $"¿Está seguro que desea establecer el docente { SituacionRevistaProfesor.NombreCompleto } a cargo de la materia { Descripcion }?";
                caption = "Docente a Cargo de la Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var situacionRevistaEnFunciones = _profesores.Where(x => x.EnFunciones && x.FechaBaja is null)
                                                                     .FirstOrDefault();
                        QuitarDocenteEnFunciones(situacionRevistaEnFunciones);

                        _crearDocenteEnFunciones = new CrearDocenteEnFuncionesRequest(_materiaResponse.Curso, _materiaResponse.Materia, SituacionRevistaProfesor.DocenteID );
                        _servicioCursos.EstablecerDocenteEnFunciones(_crearDocenteEnFunciones);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        AgregarDocenteEnFunciones(SituacionRevistaProfesor.DocenteID);
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

    #region HorarioCommand
    private bool CanExecuteHorarioCommand(object obj) => !EditarMateria;

    private void ExecuteHorarioCommand(object obj)
    {
        throw new NotImplementedException();
    }
    #endregion
}
