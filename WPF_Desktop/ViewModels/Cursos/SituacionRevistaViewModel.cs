using Core.ServicioCursos.DTOs.Responses;
using Domain.Cursos.Materias;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class SituacionRevistaViewModel : ViewModel, INotifyDataErrorInfo
{
    private SituacionRevistaResponse _situacionRevista;

    private Guid _docenteID = Guid.Empty;
    private string _nombreCompleto = string.Empty;
    private int _cargo = 0;
    private string _cargoDescripcion = string.Empty;
    private DateTime _fechaAlta = DateTime.Now;
    private DateTime? _fechaBaja;
    private bool _enFunciones;


    private Dictionary<string, List<string>> _errorsByProperty = new();

    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public SituacionRevistaViewModel(SituacionRevistaResponse situacionRevista)
    {
        Cargo = 0;
        FechaAlta = DateTime.Now;
        EnFunciones = false;

        if (situacionRevista is not null)
        {
            _situacionRevista = situacionRevista;
            DocenteID = _situacionRevista.DocenteID;
            NombreCompleto = _situacionRevista.NombreCompleto;
            Cargo = _situacionRevista.Cargo;
            FechaAlta = _situacionRevista.FechaAlta;
            FechaBaja = _situacionRevista.FechaBaja;
            EnFunciones = _situacionRevista.EnFunciones;
        }
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

    public string NombreCompleto
    {
        get
        {
            return _nombreCompleto;
        }

        set
        {
            _nombreCompleto = value;
            OnPropertyChanged(nameof(NombreCompleto));
        }
    }

    public int Cargo
    {
        get
        {
            return _cargo;
        }

        set
        {
            _cargo = value;
            base.OnPropertyChanged(nameof(Cargo));
        }
    }

    public string CargoDescripcion
    {
        get
        {
            return Enum.GetName(typeof(Cargo), Cargo);
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
