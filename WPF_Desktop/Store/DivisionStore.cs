using System;
using WPF_Desktop.ViewModels.Cursos.Divisiones;

namespace WPF_Desktop.Store;

public class DivisionStore
{
    private DivisionViewModel _division = null;
    public event Action DivisionStoreChanged;

 
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
