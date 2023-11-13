using Core.ServicioDocentes.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

public class DocenteInfoViewModel : ViewModel
{
    private DocenteInfoResponse _docenteResponse;

    private Guid _docenteID = Guid.Empty;
    private string _nombreCompleto = string.Empty;
    private DocenteInstitucionalViewModel _institucional;


    public DocenteInfoViewModel(DocenteInfoResponse docenteResponse)
    {
        if (docenteResponse is not null)
        {
            _docenteResponse = docenteResponse;
            DocenteID = docenteResponse.DocenteID;
            NombreCompleto = docenteResponse.NombreCompleto;
            Institucional = new DocenteInstitucionalViewModel(docenteResponse.Institucional);
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

    public DocenteInstitucionalViewModel Institucional
    {
        get
        {
            return _institucional;
        }

        set
        {
            _institucional = value;
            OnPropertyChanged(nameof(Institucional));
        }
    }
    #endregion
}
