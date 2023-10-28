using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
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
    private readonly MateriaResponse _materiaResponse = null;
    private Dictionary<string, List<string>> _errorsByProperty = new();

    private bool _editarMateria = false;
    private Guid _materia = Guid.Empty;
    private string _descripcion = string.Empty;
    private int _horasCatedra;
    private Guid? _profesor = Guid.Empty;
    private ObservableCollection<ProfesorViewModel> _profesores = new();
    private ObservableCollection<HorarioViewModel> _horarios = new();

    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand CancelarCommand { get; }
    public ViewModelCommand EditarCommand { get; }
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
            Profesor = _materiaResponse.Profesor;

            if (_materiaResponse.Profesores.Count > 0)
            {
                foreach (SituacionRevistaProfesorResponse response in _materiaResponse.Profesores)
                {
                    Profesores.Add(new ProfesorViewModel(response));
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
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        HorarioCommand = new ViewModelCommand(ExecuteHorarioCommand, CanExecuteHorarioCommand);
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

    public Guid? Profesor
    {
        get
        {
            return _profesor;
        }
        set
        {
            _profesor = value;
            OnPropertyChanged(nameof(Profesor));
        }
    }

    public int CantidadProfesores
    {
        get
        {
            return _profesores.Count();
        }
    }

    public ObservableCollection<ProfesorViewModel> Profesores
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
        Profesor = _materiaResponse.Profesor;
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

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj) => EditarMateria;

    private void ExecuteGuardarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        messageBoxText = $"¿Está seguro que desea guardar los cambios en la materia?";
        caption = "Guardar Materia";
        result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var request = new EditarMateriaRequest(_materiaResponse.Curso, _materiaResponse.Materia, Descripcion, HorasCatedra);
                _servicioCursos.ActualizarMateria(request);

                messageBoxText = $"Cambios guardados exitósamente.";
                caption = "Operación Exitosa";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
