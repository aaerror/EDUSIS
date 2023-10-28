using Core.ServicioCursos.DTOs.Responses;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Cursos;

public class CursoViewModel : ViewModel
{
    private CursoResponse _curso;


    public CursoViewModel(CursoResponse cursoResponses)
    {
        _curso = cursoResponses;
    }

    #region Properties
    public Guid CursoId
    {
        get
        {
            return _curso.CursoId;
        }

        set
        {
            _curso.CursoId = value;
            OnPropertyChanged(nameof(CursoId));
        }
    }

    public string Descripcion
    {
        get
        {
            return _curso.Descripcion;
        }

        set
        {
            _curso.Descripcion = value;
            OnPropertyChanged(nameof(Descripcion));
        }
    }

    public string NivelEducativo
    {
        get
        {
            return _curso.NivelEducativo;
        }

        set
        {
            _curso.NivelEducativo = value;
            OnPropertyChanged(nameof(NivelEducativo));
        }
    }

    public int Divisiones
    {
        get
        {
            return _curso.Divisiones;
        }

        set
        {
            _curso.Divisiones = value;
            OnPropertyChanged(nameof(Divisiones));
        }
    }

    public int Materias
    {
        get
        {
            return _curso.Materias;
        }

        set
        {
            _curso.Materias = value;
            OnPropertyChanged(nameof(Materias));
        }
    }

    public int Alumnos
    {
        get
        {
            return _curso.Alumnos;
        }

        set
        {
            _curso.Alumnos = value;
            OnPropertyChanged(nameof(Alumnos));
        }
    }
    #endregion
}
