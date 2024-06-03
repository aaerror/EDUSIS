using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioMaterias;
using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using Domain.Materias.SituacionRevistaDocente;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using WPF_Desktop.ViewModels.Docentes;

namespace WPF_Desktop.ViewModels.Materias;

public class GestionMateriasViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioMateria _servicioMaterias;
    private readonly IServicioDocente _servicioDocentes;
    private CursoStore _cursoStore;

    private DocenteInstitucionalViewModel _docenteInstitucionalViewModel;
    private MateriaViewModel _materiaDetalleViewModel;
    private SituacionRevistaViewModel _situacionRevistaViewModel;

    #region Request
    private RegistrarMateriaRequest _registrarMateriaRequest = null;
    private RegistrarSituacionRevistaRequest _registrarSituacionRevistaRequest = null;
    #endregion

    #region Response
    private DocenteInfoResponse _docenteInfoResponse = null;
    private SituacionRevistaResponse _situacionRevistaResponse = null;
    #endregion

    public ObservableCollection<MateriaViewModel> _materias = new ObservableCollection<MateriaViewModel>();
    private string _headerTitle = string.Empty;

    private string _descripcion = string.Empty;
    private int _horasCatedraRegistrar = 0;

    #region Horario
    private int _diaSemana = 0;
    private int _turno = 0;
    private string _duracion = string.Empty;
    private string _horaInico = string.Empty;
    #endregion

    #region Profesor
    private string _buscarProfesor = string.Empty;
    private Guid _profesorID = Guid.Empty;
    #endregion

    private bool _habilitarEditarMateria = false;
    private bool _habilitarRegistrarMateria = false;
    private bool _habilitarRegistrarHorario = false;
    private bool _habilitarRegistrarProfesor = false;


    public DateTime FechaHoy { get; } = DateTime.Now;


    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();

    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand BuscarCommand { get; }
    public ViewModelCommand CancelarCommand { get; }
    public ViewModelCommand EditarCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand RegistrarCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    #endregion


    public GestionMateriasViewModel(IServicioMateria servicioMaterias, IServicioDocente servicioDocentes, CursoStore cursoStore)
    {
        _servicioMaterias = servicioMaterias;
        _servicioDocentes = servicioDocentes;
        _cursoStore = cursoStore;

        HeaderTitle = $"{_cursoStore.Curso.Descripcion} {"año".ToUpper()} ({_cursoStore.Curso.NivelEducativo})";

        HabilitarEditarMateria = false;
        HabilitarRegistrarMateria = true;
        HabilitarRegistrarHorario = false;
        HabilitarRegistrarProfesor = false;

        BuscarCommand = new ViewModelCommand(ExecuteBuscarCommand, CanExecuteBuscarCommand);
        CancelarCommand = new ViewModelCommand(ExecuteCancelarCommand, CanExecuteCancelarCommand);
        EditarCommand = new ViewModelCommand(ExecuteEditarCommand, CanExecuteEditarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        RegistrarCommand = new ViewModelCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);

        LoadMaterias();
    }

    private void LoadMaterias()
    {
        try
        {
            var lista = _servicioMaterias.BuscarMateriaSegunCurso(new BuscarMateriaSegunCursoRequest(_cursoStore.Curso.CursoID));
            if (lista.Count == 0)
            {
                string messageBoxText = "No existen materias adicionadas para este curso.";
                string caption = "Diseño de Curricula";

                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                _materias.Clear();
                foreach (MateriaResponse materia in lista)
                {
                    _materias.Add(new MateriaViewModel(_servicioMaterias, materia));
                }
            }
        }
        catch (Exception ex)
        {
            string messageBoxText = $"Error al cargar las materias del curso.\nError: {ex.Message}";
            MessageBox.Show(messageBoxText, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public string HeaderTitle
    {
        get
        {
            return _headerTitle;
        }

        set
        {
            _headerTitle = value;
            OnPropertyChanged(nameof(HeaderTitle));
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

    public bool HabilitarRegistrarHorario
    {
        get
        {
            return _habilitarRegistrarHorario;
        }

        set
        {
            _habilitarRegistrarHorario = value;
            OnPropertyChanged(nameof(HabilitarRegistrarHorario));
        }
    }

    public bool HabilitarRegistrarProfesor
    {
        get
        {
            return _habilitarRegistrarProfesor;
        }

        set
        {
            _habilitarRegistrarProfesor = value;
            OnPropertyChanged(nameof(HabilitarRegistrarProfesor));
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

    public MateriaViewModel MateriaDetalleViewModel
    {
        get
        {
            return _materiaDetalleViewModel;
        }

        set
        {
            _materiaDetalleViewModel = value;
            OnPropertyChanged(nameof(MateriaDetalleViewModel));
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
            _descripcion = value;
            OnPropertyChanged(nameof(Descripcion));
            _errorsByProperty.Remove(nameof(Descripcion));

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                _errorsByProperty.Add(nameof(Descripcion), new List<string>()
                {
                    "Se debe especificar el nombre de la materia."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Descripcion)));
            }
            else
            {
                var esDuplicado = _servicioMaterias.EsNombreDuplicadoMateria(new NombreDuplicadoRequest(_cursoStore.Curso.CursoID, Descripcion));
                if (esDuplicado)
                {
                    _errorsByProperty.Add(nameof(Descripcion), new List<string>()
                    {
                        "Nombre de materia duplicado."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Descripcion)));
                }
            }
        }
    }

    public int HorasCatedraRegistrar
    {
        get
        {
            return _horasCatedraRegistrar;
        }

        set
        {
            _horasCatedraRegistrar = value;
            OnPropertyChanged(nameof(HorasCatedraRegistrar));
            _errorsByProperty.Remove(nameof(HorasCatedraRegistrar));

            if (HorasCatedraRegistrar < 0)
            {
                _errorsByProperty.Add(nameof(HorasCatedraRegistrar), new List<string>()
                {
                    "Se deben seleccionar la cantidad de horas catedras."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(HorasCatedraRegistrar)));
            }
        }
    }
    #endregion

    #region Properties RegistrarHorario
    public int DiaSemana
    {
        get
        {
            return _diaSemana;
        }

        set
        {
            _errorsByProperty.Remove(nameof(DiaSemana));
            _diaSemana = value;
            OnPropertyChanged(nameof(DiaSemana));

            if (DiaSemana < 0)
            {
                _errorsByProperty.Add(nameof(DiaSemana), new List<string>()
                {
                    "Se debe seleccionar un día de semana."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DiaSemana)));
            }
        }
    }

    public int Turno
    {
        get
        {
            return _turno;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Turno));
            _turno = value;
            OnPropertyChanged(nameof(Turno));

            if (Turno < 0)
            {
                _errorsByProperty.Add(nameof(Turno), new List<string>()
                {
                    "Se debe seleccionar un turno."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Turno)));
            }
        }
    }

    public string HoraInicio
    {
        get
        {
            return _horaInico;
        }

        set
        {
            _errorsByProperty.Remove(nameof(HoraInicio));
            _horaInico = value;
            OnPropertyChanged(nameof(HoraInicio));

            if (string.IsNullOrWhiteSpace(HoraInicio))
            {
                _errorsByProperty.Add(nameof(HoraInicio), new List<string>()
                {
                    "Se debe especificar un horario."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(HoraInicio)));
            }
            else
            {
                if (!Regex.IsMatch(HoraInicio, @"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(HoraInicio), new List<string>()
                    {
                        "Formato de horario incorrecto."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(HoraInicio)));
                }
            }
        }
    }

    public string Duracion
    {
        get
        {
            return _duracion;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Duracion));
            _duracion = value;
            OnPropertyChanged(nameof(Duracion));

            if (string.IsNullOrWhiteSpace(Duracion))
            {
                _errorsByProperty.Add(nameof(Duracion), new List<string>()
                {
                    "Se debe ingresar la duración de la hora cátedra."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Duracion)));
            }
            else
            {
                if (!Regex.IsMatch(Duracion, @"^(\d{2})$", RegexOptions.None, TimeSpan.FromMilliseconds(2000)))
                {
                    _errorsByProperty.Add(nameof(Duracion), new List<string>()
                    {
                        "Formato inválido."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Duracion)));
                }
            }
        }
    }
    #endregion

    #region Properties RegistrarProfesor
    public string BuscarProfesor
    {
        get
        {
            return _buscarProfesor;
        }

        set
        {
            _errorsByProperty.Remove(nameof(BuscarProfesor));
            _buscarProfesor = value;
            OnPropertyChanged(nameof(BuscarProfesor));

            if (string.IsNullOrWhiteSpace(BuscarProfesor))
            {
                _errorsByProperty.Add(nameof(BuscarProfesor), new List<string>()
                {
                    "Se debe ingresar el documento del profesor."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(BuscarProfesor)));
            }
            else
            {
                if (!Regex.IsMatch(BuscarProfesor, @"^\d{8}$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(BuscarProfesor), new List<string>()
                    {
                        "Formato de número de documento incorrecto. Formato válido: ########"
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(BuscarProfesor)));
                }
            }
        }
    }

    public DocenteInstitucionalViewModel DocenteInstitucionalViewModel
    {
        get
        {
            return _docenteInstitucionalViewModel;
        }

        set
        {
            _docenteInstitucionalViewModel = value;
            OnPropertyChanged(nameof(DocenteInstitucionalViewModel));
        }
    }

    public SituacionRevistaViewModel SituacionRevistaViewModel
    {
        get
        {
            return _situacionRevistaViewModel;
        }

        set
        {
            _situacionRevistaViewModel = value;
            OnPropertyChanged(nameof(SituacionRevistaViewModel));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName);
    #endregion

    #region BuscarCommand
    private bool CanExecuteBuscarCommand(object obj)
    {
        return !HasErrors;
    }

    private void ExecuteBuscarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (string.IsNullOrWhiteSpace(BuscarProfesor))
        {
            messageBoxText = "Se debe ingresar el número de documento del profesor que desea buscar.";
            caption = "Error";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            BuscarProfesor = string.Empty;

            return;
        }

        try
        {
            _docenteInfoResponse = _servicioDocentes.BuscarDocentePorDNI(BuscarProfesor);

            DocenteInstitucionalViewModel = new(_docenteInfoResponse.Institucional);
            SituacionRevistaViewModel = new(null);
            SituacionRevistaViewModel.NombreCompleto = _docenteInfoResponse.NombreCompleto;
            _profesorID = _docenteInfoResponse.DocenteID;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region CancelarCommand
    private bool CanExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "EditarMateria":
                return HabilitarEditarMateria;
            case "Horario":
                return HabilitarRegistrarHorario;
            case "Profesor":
                return HabilitarRegistrarProfesor;
            default:
                return false;
        }
    }

    private void ExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "EditarMateria":
                HabilitarEditarMateria = false;
                HabilitarRegistrarHorario = false;
                MateriaDetalleViewModel.EditarMateria = false;

                LoadMaterias();
                break;
            case "Horario":
                HabilitarRegistrarHorario = false;
                break;
            case "Profesor":
                HabilitarRegistrarProfesor = false;
                break;
            default:
                break;
        }
    }
    #endregion

    #region EditarCommand
    private bool CanExecuteEditarCommand(object obj)
    {
        switch (obj)
        {
            case "Materia":
                return MateriaDetalleViewModel is not null && !HabilitarEditarMateria && !HabilitarRegistrarHorario && !HabilitarRegistrarProfesor;
            default: return false;
        }
    }

    private void ExecuteEditarCommand(object obj)
    {
        switch (obj)
        {
            case "Materia":
                HabilitarEditarMateria = true;
                MateriaDetalleViewModel.EditarMateria = true;

                //_editarMateriaRequest = new EditarMateriaRequest(Materia.Materia, Materia.Descripcion, Materia.HorasCatedra);
                break;
            default:
                break;
        }
    }
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj)
    {
        switch (obj)
        {
            case "EditarMateria":
                return HabilitarEditarMateria;
            case "Horario":
                return HabilitarRegistrarHorario && !HasErrors;
            case "Profesor":
                return HabilitarRegistrarProfesor && !HasErrors;
            default: return false;
        }
    }

    private void ExecuteGuardarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "EditarMateria":
                messageBoxText = $"¿Está seguro que desea guardar los cambios en la materia?";
                caption = "Guardar Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var request = new ModificarMateriaRequest(_cursoStore.Curso.CursoID,
                                                                  MateriaDetalleViewModel.Materia,
                                                                  MateriaDetalleViewModel.Descripcion,
                                                                  MateriaDetalleViewModel.HorasCatedra);
                        _servicioMaterias.ModificarMateria(request);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";

                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        HabilitarEditarMateria = false;
                        MateriaDetalleViewModel.EditarMateria = false;
                        //LoadMaterias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                        HabilitarEditarMateria = false;
                        MateriaDetalleViewModel.EditarMateria = false;
                    }
                }
                break;
            case "Horario":
                if (string.IsNullOrWhiteSpace(HoraInicio))
                {
                    messageBoxText = "Se debe ingresar la hora de inicio para continuar.";
                    caption = "Error al Registrar un Horario";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                    Duracion = string.Empty;
                    HoraInicio = string.Empty;
                }
                else
                {
                    try
                    {
                        var request = new RegistrarHorarioEnMateria(_cursoStore.Curso.CursoID,
                                                                    MateriaDetalleViewModel.Materia,
                                                                    Turno,
                                                                    DiaSemana,
                                                                    HoraInicio,
                                                                    int.Parse(Duracion));
                        _servicioMaterias.RegistrarHorario(request);

                        messageBoxText = $"Nueva horario agregado a la materia.";
                        caption = "Operación Exitosa";

                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadMaterias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                break;
            case "Profesor":
                if (_docenteInfoResponse is null)
                {
                    messageBoxText = "Se debe buscar previamente que profesor desea inscribir en la materia.";
                    caption = "Error al Cambiar Situación de Revista";

                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                    BuscarProfesor = string.Empty;
                    _docenteInstitucionalViewModel = new DocenteInstitucionalViewModel(null);

                    return;
                }

                messageBoxText = $"¿Está seguro que desea guardar los cambios en la materia?\n" +
                                 $"Se va a cambiar la situación de revista del profesor {_docenteInfoResponse.NombreCompleto}\n\n" +
                                 $"Materia: {MateriaDetalleViewModel.Descripcion}\n" +
                                 $"Cargo nuevo: {(Cargo) SituacionRevistaViewModel.Cargo}\n" +
                                 $"Fecha Alta: {SituacionRevistaViewModel.FechaAlta.Date.ToString("D")}";
                caption = "Nueva Situación de Revista";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _registrarSituacionRevistaRequest = new RegistrarSituacionRevistaRequest(_cursoStore.Curso.CursoID,
                                                                                                 MateriaDetalleViewModel.Materia,
                                                                                                 _profesorID,
                                                                                                 SituacionRevistaViewModel.Cargo,
                                                                                                 SituacionRevistaViewModel.FechaAlta,
                                                                                                 SituacionRevistaViewModel.EnFunciones);
                        _situacionRevistaResponse = _servicioMaterias.RegistrarDocenteEnMateria(_registrarSituacionRevistaRequest);

                        messageBoxText = $"¡Cambios guardados exitósamente!";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        var situacionRevistaOld = MateriaDetalleViewModel.Historial.Where(x => x.EnFunciones && x.FechaBaja is null)
                                                                                    .FirstOrDefault();

                        MateriaDetalleViewModel.Historial.Add(new SituacionRevistaViewModel(_situacionRevistaResponse));
                        MateriaDetalleViewModel.QuitarDocenteEnFunciones(situacionRevistaOld);
                        MateriaDetalleViewModel.AgregarDocenteEnFunciones(_situacionRevistaResponse.DocenteID);

                        HabilitarEditarMateria = false;
                        HabilitarRegistrarProfesor = false;
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

    #region RegistrarCommand
    private bool CanExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Materia":
                if (HasErrors)
                {
                    return false;
                }

                return HabilitarRegistrarMateria;
            case "Horario":
                return MateriaDetalleViewModel is not null && !HabilitarEditarMateria && !HabilitarRegistrarHorario;
            case "Profesor":
                return MateriaDetalleViewModel is not null && !HabilitarEditarMateria && !HabilitarRegistrarHorario;
            default:
                return true;
        }
    }

    private async void ExecuteRegistrarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Materia":
                if (string.IsNullOrWhiteSpace(Descripcion) || HorasCatedraRegistrar == 0)
                {
                    Descripcion = string.Empty;
                    HorasCatedraRegistrar = 0;

                    messageBoxText = $"Error al registrar una materia en el curso. Existen algunos campos vacíos, debe completarlos antes de continuar.";
                    caption = "Error al Registrar Materia";
                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                    break;
                }

                messageBoxText = $"Está seguro que desea adicionar la materia {Descripcion} (Curso: {_cursoStore.Curso.Descripcion}° Año | Nivel Educativo: {_cursoStore.Curso.NivelEducativo})?";
                caption = "Registrar Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _registrarMateriaRequest = new RegistrarMateriaRequest(_cursoStore.Curso.CursoID, Descripcion, HorasCatedraRegistrar + 1);
                        await _servicioMaterias.RegistrarMateria(_registrarMateriaRequest);

                        messageBoxText = $"Nueva materia agregada a la curricula del curso.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadMaterias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                break;
            case "Horario":
                HabilitarRegistrarHorario = true;
                break;
            case "Profesor":
                HabilitarRegistrarProfesor = true;
                break;
        }
    }
    #endregion

    #region EliminarCommand
    private bool CanExecuteEliminarCommand(object obj)
    {
        return MateriaDetalleViewModel is not null && !HabilitarEditarMateria && !HabilitarRegistrarHorario;
    }

    private void ExecuteEliminarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Docente":
                messageBoxText = $"¿Está seguro que desea dar de baja del cargo al docente {_docenteInfoResponse.NombreCompleto}" +
                    $"{MateriaDetalleViewModel.Descripcion} (Curso: {_cursoStore.Curso.Descripcion} año | Nivel Educativo: {_cursoStore.Curso.NivelEducativo})?";
                caption = "Eliminar Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                }

                break;

            case "Materia":
                messageBoxText = $"Está seguro que desea eliminar la materia de {MateriaDetalleViewModel.Descripcion} (Curso: {_cursoStore.Curso.Descripcion} año | Nivel Educativo: {_cursoStore.Curso.NivelEducativo})?";
                caption = "Eliminar Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var request = new EliminarMateriaRequest(_cursoStore.Curso.CursoID, MateriaDetalleViewModel.Materia);
                        _servicioMaterias.EliminarMateria(request);

                        messageBoxText = $"La materia se eliminó correctamente de la currícula del curso.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        _materias.Remove(MateriaDetalleViewModel);
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
}
