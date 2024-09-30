using Core.ServicioMaterias.DTOs.Responses;
using Domain.Materias.CargosDocentes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;

public class SituacionRevistaViewModel : ViewModel, INotifyDataErrorInfo
{
    private SituacionRevistaResponse _situacionRevista;

    private Guid _docenteID;
    private string _docente;
    private Cargo _cargo;
    private DateTime _fechaAlta = DateTime.Now;
    private DateTime? _fechaBaja;
    private bool _enFunciones;

    private Dictionary<string, List<string>> _errorsByProperty = new();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public SituacionRevistaViewModel(SituacionRevistaResponse situacionRevista)
    {
        /*FechaAlta = DateTime.Now;
        EnFunciones = true;*/

        if (situacionRevista is not null)
        {
            _situacionRevista = situacionRevista;
            DocenteID = _situacionRevista.DocenteID;
            Docente = _situacionRevista.Docente;
            Cargo = _situacionRevista.Cargo;
            FechaAlta = _situacionRevista.FechaAlta;
            FechaBaja = _situacionRevista.FechaBaja;
            EnFunciones = _situacionRevista.EnFunciones;
        }
    }

    public SituacionRevistaViewModel(Guid docenteID, string docente)
    {
        DocenteID = docenteID;
        Docente = docente;
        EnFunciones = true;
    }

    #region Properties
    public Guid DocenteID
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

    public string Docente
    {
        get
        {
            return _docente;
        }

        set
        {
            _docente = value;
            OnPropertyChanged(nameof(Docente));
        }
    }

    public Cargo Cargo
    {
        get
        {
            return _cargo;
        }

        set
        {
            _cargo = value;
            OnPropertyChanged(nameof(Cargo));
        }
    }

    public DateTime FechaAlta
    {
        get
        {
            return _fechaAlta;
        }

        set
        {
            _fechaAlta = value;
            OnPropertyChanged(nameof(FechaAlta));
        }
    }

    public DateTime? FechaBaja
    {
        get
        {
            return _fechaBaja;
        }

        set
        {
            _fechaBaja = value;
            OnPropertyChanged(nameof(FechaBaja));
        }
    }

    public bool EnFunciones
    {
        get
        {
            return _enFunciones;
        }

        set
        {
            _enFunciones = value;
            OnPropertyChanged(nameof(EnFunciones));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion
}
