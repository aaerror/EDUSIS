using Core.ServicioCursos;
using Core.ServicioCursos.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

public class CalificacionViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioCursos _servicioCursos;
    private CalificacionResponse _calificacionResponse;

    private Guid _materiaID;
    private string _materia;
    private bool _asistencia;
    private DateTime? _fecha;
    private int _instancia;
    private double? _nota;

    private bool _mostrarNota;

    private ObservableCollection<MateriaResponse> _materias;

    private Dictionary<string, List<string>> _errorsByProperty = new();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public CalificacionViewModel(IServicioCursos servicioCursos, CalificacionResponse calificacionResponse)
    {
        _servicioCursos = servicioCursos;
        Materia = string.Empty;
        Asistencia = false;
        Fecha = DateTime.Today;
        Instancia = 0;

        if (calificacionResponse is not null)
        {
            _calificacionResponse = calificacionResponse;
            MateriaID = _calificacionResponse.MateriaID;
            Materia = _calificacionResponse.Materia;
            Asistencia = _calificacionResponse.Asistencia;
            Fecha = _calificacionResponse.Fecha;
            Instancia = _calificacionResponse.Instancia;
            Nota = _calificacionResponse.Nota;
        }
    }

    #region Properties
    public Guid MateriaID
    {
        get
        {
            return _materiaID;
        }

        set
        {
            _materiaID = value;
            OnPropertyChanged(nameof(MateriaID));
        }
    }

    public string Materia
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

    public bool Asistencia
    {
        get
        {
            return _asistencia;
        }

        set
        {
            _asistencia = value;
            OnPropertyChanged(nameof(Asistencia));

            if (Asistencia)
            {
                MostrarNota = true;
            }
            else
            {
                MostrarNota = false;
            }
        }
    }

    public DateTime? Fecha
    {
        get
        {
            return _fecha;
        }

        set
        {
            _fecha = value;
            OnPropertyChanged(nameof(Fecha));
        }
    }

    public int Instancia
    {
        get
        {
            return _instancia;
        }

        set
        {
            _instancia = value;
            OnPropertyChanged(nameof(Instancia));
        }
    }

    public double? Nota
    {
        get
        {
            return _nota;
        }

        set
        {
            _nota = value;
            OnPropertyChanged(nameof(Nota));
        }
    }

    public bool MostrarNota
    {
        get
        {
            return _mostrarNota;
        }

        set
        {
            _mostrarNota = value;
            OnPropertyChanged(nameof(MostrarNota));
        }
    }

    public ObservableCollection<MateriaResponse> Materias
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
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName);
    #endregion
}
