using Core.ServicioCursos.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class CursoViewModel : ViewModel
{
    private CursoResponse _cursoResponse;

    private Guid _cursoID;
    private string _descripcion;
    private string _nivelEducativo;
    private string _divisiones;
    private string _materias;
    private string _alumnos;


    public CursoViewModel(CursoResponse cursoResponses)
    {
        CursoID = Guid.Empty;
        Descripcion = string.Empty;
        NivelEducativo = string.Empty;
        Divisiones = string.Empty;
        Materias = string.Empty;
        Alumnos = string.Empty;

        if (cursoResponses is not null)
        {
            _cursoResponse = cursoResponses;
            CursoID = _cursoResponse.CursoID;
            Descripcion = _cursoResponse.Descripcion;
            NivelEducativo = _cursoResponse.NivelEducativo;
            Divisiones = _cursoResponse.Divisiones.ToString();
            Materias = _cursoResponse.Materias.ToString();
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

    public string Materias
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
