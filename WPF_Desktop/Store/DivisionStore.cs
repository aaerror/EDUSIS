using System;
using WPF_Desktop.ViewModels.Cursos.Divisiones;

namespace WPF_Desktop.Store;

public class DivisionStore
{
    private CursoStore _cursoStore = null;
    private DivisionViewModel _division = null;
    public event Action DivisionStoreChanged;

 
    public CursoStore Curso
    {
        get
        {
            return _cursoStore;
        }

        set
        {
            _cursoStore = value;
            DivisionStoreChanged?.Invoke();
        }
    }

    public DivisionViewModel Division
    {
        get
        {
            return _division; 
        }

        set
        {
            _division = value;
            DivisionStoreChanged?.Invoke();
        }
    }
}
