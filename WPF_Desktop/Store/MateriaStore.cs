using System;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

namespace WPF_Desktop.Store;

public class MateriaStore
{
    private MateriaViewModel _materiaViewModel = null;

    public event Action MateriaStoreChanged;


    public MateriaViewModel Materia
    {
        get
        {
            return _materiaViewModel;
        }

        set
        {
            _materiaViewModel = value;
            MateriaStoreChanged?.Invoke();
        }
    }
}
