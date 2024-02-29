using Core.ServicioCursos;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;
using System.Collections;
using System.Linq;
using Core.ServicioCursos.DTOs.Requests;
using System.Windows;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

public class GestionCursantesViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioCursos _servicioCursos;
    private readonly DivisionStore _divisionStore;

    #region Request
    private BuscarListadoRequest _buscarListadoRequest;
    #endregion

    private string _periodo = string.Empty;

    private bool _mostrarCalificacionView;
    private MateriaDetalleViewModel _materia;
    private ObservableCollection<MateriaDetalleViewModel> _materias;
    private CalificacionViewModel _calificacion;
    private CursanteViewModel _cursante;
    private ObservableCollection<CursanteViewModel> _cursantes;

    private Dictionary<string, List<string>> _errorsByProperty = new();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;



    #region Commands
    public ViewModelCommand BuscarCommand { get; }
    public ViewModelCommand ABMCommand { get; }
    #endregion



    public GestionCursantesViewModel(IServicioCursos servicioCursos, DivisionStore divisionStore)
    {
        _servicioCursos = servicioCursos;
        _divisionStore = divisionStore;

        MostrarCalificacionView = false;

        var materias = _servicioCursos.BuscarMaterias(divisionStore.Division.CursoID);
        _materias = new ObservableCollection<MateriaDetalleViewModel>(materias.Select(x => new MateriaDetalleViewModel(_servicioCursos, x)));

        BuscarCommand = new ViewModelCommand(ExecuteBuscarCommand, CanExecuteBuscarCommand);
        ABMCommand = new ViewModelCommand(ExecuteABMCommand, CanExecuteABMCommand);
    }

    private void BuscarCursantes(string periodo)
    {
        _buscarListadoRequest = new BuscarListadoRequest(_divisionStore.Division.CursoID, _divisionStore.Division.DivisionID, periodo);
        var listado = _servicioCursos.BuscarListado(_buscarListadoRequest);
        Cursantes = new ObservableCollection<CursanteViewModel>(listado.Select(x => new CursanteViewModel(x)));
    }

    public bool MostrarCalificacionView
    {
        get
        {
            return _mostrarCalificacionView;
        }

        set
        {
            _mostrarCalificacionView = value;
            OnPropertyChanged(nameof(MostrarCalificacionView));
        }
    }

    #region Properties
    public string Periodo
    {
        get
        {
            return _periodo;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Periodo));
            _periodo = value;
            OnPropertyChanged(nameof(Periodo));

            if (string.IsNullOrWhiteSpace(Periodo))
            {
                _errorsByProperty.Add(nameof(Periodo), new List<string>
                {
                    "Debe ingresar el año para buscar el listado de alumnos de dicho ciclo lectivo."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Periodo)));
            }
            else
            {
                if (!Regex.IsMatch(Periodo, @"^(20)\d{2}$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Periodo), new List<string>
                    {
                        "Formato inválido del año. Puede ingresar un año comprendido entre el 2000 y el 2099."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Periodo)));
                }
            }
        }
    }

    public MateriaDetalleViewModel Materia
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

    public ObservableCollection<MateriaDetalleViewModel> Materias
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

    public CalificacionViewModel Calificacion
    {
        get
        {
            return _calificacion;
        }

        set
        {
            _calificacion = value;
            OnPropertyChanged(nameof(Calificacion));
        }
    }

    public CursanteViewModel Cursante
    {
        get
        {
            return _cursante;
        }

        set
        {
            _cursante = value;
            OnPropertyChanged(nameof(Cursante));
        }
    }

    public ObservableCollection<CursanteViewModel> Cursantes
    {
        get
        {
            return _cursantes;
        }

        set
        {
            _cursantes = value;
            OnPropertyChanged(nameof(Cursantes));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion

    #region ABMCommand
    private bool CanExecuteABMCommand(object obj)
    {
        switch (obj)
        {
            case "Calificacion":
                return Cursante is not null;
            default:
                return false;
        }
    }

    private void ExecuteABMCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Calificacion":
                MostrarCalificacionView = true;
                Calificacion = new CalificacionViewModel(_servicioCursos, null);
                break;
        }
    }
    #endregion

    #region BuscarCommand
    private bool CanExecuteBuscarCommand(object obj)
    {
        switch (obj)
        {
            case "Listado":
                return !_errorsByProperty.ContainsKey(nameof(Periodo));
            default:
                return false;
        }
    }

    private void ExecuteBuscarCommand(object obj)
    {
        switch (obj)
        {
            case "Listado":
                BuscarCursantes(Periodo);
                break;
        }
    }
    #endregion
}
