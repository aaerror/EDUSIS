using Core.ServicioCusos.DTOs.Responses;
using System;
using System.Windows.Forms;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos.Divisiones;

public class DivisionViewModel : ViewModel
{
    #region Response
    private DivisionResponse _divisionResponse;
    #endregion

    private Guid _cursoID;
    private string _cursoDescripcion;
    private string _nivelEducativo;
    private Guid _divisionID;
    private string _divisionDescripcion;
    private string _alumnos;


    public DivisionViewModel(DivisionResponse divisionResponse)
    {
        CursoDescripcion = string.Empty;
        NivelEducativo = string.Empty;
        DivisionDescripcion = string.Empty;

        if (divisionResponse is not null)
        {
            _divisionResponse = divisionResponse;
            CursoID = divisionResponse.CursoID;
            CursoDescripcion = divisionResponse.CursoDescripcion;
            NivelEducativo = divisionResponse.NivelEducativo;
            DivisionID = divisionResponse.DivisionID;
            DivisionDescripcion = divisionResponse.DivisionDescripcion;
            Alumnos = divisionResponse.Alumnos.ToString();
        }
    }

    #region Properties
    public Guid CursoID
    {
        get
        {
            return _cursoID;
        }

        set
        {
            _cursoID = value;
            OnPropertyChanged(nameof(CursoID));
        }
    }

    public string CursoDescripcion
    {
        get
        {
            return _cursoDescripcion;
        }

        set
        {
            _cursoDescripcion = value;
            OnPropertyChanged(nameof(CursoDescripcion));
        }
    }

    public string NivelEducativo
    {
        get
        {
            return _nivelEducativo;
        }

        set
        {
            _nivelEducativo = value;
            OnPropertyChanged(nameof(NivelEducativo));
        }
    }

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

    public string DivisionDescripcion
    {
        get
        {
            return _divisionDescripcion;
        }

        set
        {
            _divisionDescripcion = value;
            OnPropertyChanged(nameof(DivisionDescripcion));
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
