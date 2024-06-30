using Core.ServicioCusos.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

public class DivisionViewModel : ViewModel
{
    #region Response
    private DivisionResponse _divisionResponse;
    #endregion

    private Guid _divisionID;
    private string _descripcion;
    private string _preceptor;
    private string _alumnos;


    public DivisionViewModel(DivisionResponse divisionResponse)
    {
        if (divisionResponse is not null)
        {
            _divisionResponse = divisionResponse;
            DivisionID = _divisionResponse.DivisionID;
            Descripcion = _divisionResponse.Descripcion;
            Preceptor = _divisionResponse.Preceptor;
            Alumnos = _divisionResponse.Alumnos.ToString();
        }
    }

    #region Properties

    public Guid DivisionID
    {
        get
        {
            return _divisionID;
        }

        set
        {
            _divisionID = value;
            OnPropertyChanged(nameof(DivisionID));
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
        }
    }

    public string Preceptor
    {
        get
        {
            return _preceptor;
        }

        set
        {
            _preceptor = value;
            OnPropertyChanged(nameof(Preceptor));
        }
    }

    public string Alumnos
    {
        get
        {
            return _alumnos;
        }

        set
        {
            _alumnos = value;
            OnPropertyChanged(nameof(Alumnos));
        }
    }
    #endregion

}
