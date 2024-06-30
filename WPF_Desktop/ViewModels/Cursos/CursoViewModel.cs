using Core.ServicioCursos.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class CursoViewModel : ViewModel
{
    private CursoResponse _cursoResponse;

    private Guid _cursoID;
    private int _grado;
    private string _gradoDescripcion;
    private int _nivelEducativo;
    private string _nivelEducativoDescripcion;
    private string _divisiones;
    private string _alumnos;


    public CursoViewModel(CursoResponse cursoResponses)
    {
        CursoID = Guid.Empty;
        GradoDescripcion = string.Empty;
        NivelEducativoDescripcion = string.Empty;
        Divisiones = string.Empty;
        Alumnos = string.Empty;

        if (cursoResponses is not null)
        {
            _cursoResponse = cursoResponses;
            CursoID = _cursoResponse.CursoID;
            Grado = _cursoResponse.Grado;
            GradoDescripcion = _cursoResponse.GradoDescripcion;
            NivelEducativo = _cursoResponse.NivelEducativo;
            NivelEducativoDescripcion = _cursoResponse.NivelEducativoDescripcion;
            Divisiones = _cursoResponse.Divisiones.ToString();
            Alumnos = _cursoResponse.Alumnos.ToString();
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

    public int Grado
    {
        get
        {
            return _grado;
        }

        set
        {
            _grado = value;
            OnPropertyChanged(nameof(Grado));
        }
    }

    public string GradoDescripcion
    {
        get
        {
            return _gradoDescripcion;
        }

        set
        {
            _gradoDescripcion = value;
            OnPropertyChanged(nameof(GradoDescripcion));
        }
    }

    public int NivelEducativo
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

    public string NivelEducativoDescripcion
    {
        get
        {
            return _nivelEducativoDescripcion;
        }

        set
        {
            _nivelEducativoDescripcion= value;
            OnPropertyChanged(nameof(NivelEducativoDescripcion));
        }
    }

    public string Divisiones
    {
        get
        {
            return _divisiones;
        }

        set
        {
            _divisiones = value;
            OnPropertyChanged(nameof(Divisiones));
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
