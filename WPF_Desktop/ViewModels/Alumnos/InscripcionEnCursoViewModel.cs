using Core.ServicioCursos;
using System.Collections.ObjectModel;
using System.Linq;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Cursos.Divisiones;

namespace WPF_Desktop.ViewModels.Alumnos;

public class InscripcionEnCursoViewModel : ViewModel
{
    private readonly IServicioCursos _servicioCursos;

    #region Responses

    #endregion

    private CursoViewModel _curso;
    private ObservableCollection<DivisionViewModel> _divisiones;


    public InscripcionEnCursoViewModel(IServicioCursos servicioCursos)
    {
        _servicioCursos = servicioCursos;

        var cursos = _servicioCursos.BuscarCursos();
        //_divisiones = new ObservableCollection<DivisionViewModel>(cursos.Select(x => new CursoViewModel(x)));
    }

    #region Properties
    public CursoViewModel Curso
    {
        get
        {
            return _curso;
        }

        set
        {
            _curso = value;
            OnPropertyChanged(nameof(Curso));
        }
    }

    public ObservableCollection<DivisionViewModel> Divisiones
    {
        get
        {
            return _divisiones;
        }
    }
    #endregion
}
