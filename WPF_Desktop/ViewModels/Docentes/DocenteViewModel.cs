using Core.ServicioDocentes.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

public class DocenteViewModel : ViewModel
{
    private DocenteResponse _docenteResponse;
    private Guid _docenteID;
    private string _nombreCompleto;
    private string _documento;
    private string _legajo;
    private DateTime _fechaAlta;
    private int _antiguedad;


    public DocenteViewModel(DocenteResponse docenteResponse)
    {
        if (docenteResponse is not null)
        {
            _docenteResponse = docenteResponse;
        }
    }

    #region Properties
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

    public string Documento
    {
        get
        {
            return _documento;
        }

        set
        {
            _documento = value;
            OnPropertyChanged(nameof(Documento));
        }
    }

    public string Legajo
    {
        get
        {
            return _legajo;
        }

        set
        {
            _legajo = value;
            OnPropertyChanged(nameof(Legajo));
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

    public int Antiguedad
    {
        get
        {
            return DateTime.Now.Subtract(FechaAlta).Days / 360;
        }
    }
    #endregion
}
