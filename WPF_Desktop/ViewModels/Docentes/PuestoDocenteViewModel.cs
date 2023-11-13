using Core.ServicioDocentes.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

public class PuestoDocenteViewModel : ViewModel, INotifyDataErrorInfo
{
    private PuestoResponse _puestoDocenteResponse;

    private int _posicionIndex = 0;
    private string _posicionDescripcion = string.Empty;
    private DateTime _fechaInicio;
    private DateTime? _fechaFin;

    private Dictionary<string, List<string>> _errorsByProperty = new();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public PuestoDocenteViewModel(PuestoResponse puestoDocenteResponse)
    {
        FechaInicio = DateTime.Now;
        FechaFin = null;

        if (puestoDocenteResponse is not null)
        {
            _puestoDocenteResponse = puestoDocenteResponse;
            PosicionIndex = _puestoDocenteResponse.PosicionIndex;
            PosicionDescripcion = _puestoDocenteResponse.PosicionDescripcion;
            FechaInicio = _puestoDocenteResponse.FechaInicio;
            FechaFin = _puestoDocenteResponse.FechaFin;
        }
    }

    #region Properties
    public int PosicionIndex
    {
        get
        {
            return _posicionIndex;
        }

        set
        {
            _posicionIndex = value;
            OnPropertyChanged(nameof(PosicionIndex));
        }
    }

    public string PosicionDescripcion
    {
        get
        {
            return _posicionDescripcion;
        }

        set
        {
            _posicionDescripcion = value;
            OnPropertyChanged(nameof(PosicionDescripcion));
        }
    }

    public DateTime FechaInicio
    {
        get
        {
            return _fechaInicio;
        }

        set
        {
            _errorsByProperty.Remove(nameof(FechaInicio));
            _fechaInicio = value.Date;
            OnPropertyChanged(nameof(FechaInicio));

            if (FechaInicio.Date > DateTime.Today.Date)
            {
                _errorsByProperty.Add(nameof(FechaInicio), new List<string>
                {
                    "La fecha de inicio no puede ser mayor a la fecha de hoy."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FechaInicio)));
            }
        }
    }

    public DateTime? FechaFin
    {
        get
        {
            return _fechaFin;
        }

        set
        {
            _errorsByProperty.Remove(nameof(FechaFin));
            _fechaFin = value;
            OnPropertyChanged(nameof(FechaFin));

            if (FechaFin is not null)
            {
                if (FechaFin <= FechaInicio)
                {
                    _errorsByProperty.Add(nameof(FechaFin), new List<string>
                    {
                        "La fecha de finalización no puede ser menor a la fecha de inicio."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FechaFin)));
                }
            }
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion
}
