using System;
using WPF_Desktop.ViewModels.Cursos;

namespace WPF_Desktop.Store;

public class CursoStore
{
    private CursoViewModel _curso = null;

    public event Action CursoStoreChanged;


    public CursoViewModel Curso
    {
        get
        {
            return _curso;
        }

        set
        {
            _curso = value;
            CursoStoreChanged?.Invoke();
        }
    }
}
