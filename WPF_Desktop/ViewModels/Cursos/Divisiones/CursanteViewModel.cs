using Core.ServicioCursos.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

public class CursanteViewModel : ViewModel
{
    private CursanteResponse _cursanteResponse;

    private Guid _alumnoID;
    private string _nombreCompleto;
    private string _documento;
    private string _edad;


    public CursanteViewModel(CursanteResponse cursanteResponse)
    {
        AlumnoID = Guid.Empty;
        NombreCompleto = string.Empty;
        Documento = string.Empty;
        Edad = string.Empty;

        if (cursanteResponse is not null)
        {
            _cursanteResponse = cursanteResponse;
            AlumnoID = _cursanteResponse.AlumnoID;
            NombreCompleto = _cursanteResponse.NombreCompleto;
            Documento = _cursanteResponse.Documento;
            Edad = _cursanteResponse.Edad;
        }
    }

    #region Properties
    public Guid AlumnoID
    {
        get
        {
            return _alumnoID;
        }

        set
        {
            _alumnoID = value;
            OnPropertyChanged(nameof(AlumnoID));
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

    public string Edad
    {
        get
        {
            return _edad;
        }

        set
        {
            _edad = value;
            OnPropertyChanged(nameof(Edad));
        }
    }
    #endregion
}
