using Core.ServicioAlumnos;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels;

public class EditarAlumnoViewModel : ViewModel
{
    private readonly IServicioAlumno _servicioAlumno;

    public EditarAlumnoViewModel(IServicioAlumno servicioAlumno)
    {
        _servicioAlumno = servicioAlumno;
    }
}
