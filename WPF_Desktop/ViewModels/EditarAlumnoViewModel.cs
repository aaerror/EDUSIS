using Core.ServicioAlumnos;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels;

public class EditarAlumnoViewModel : ViewModel
{
    private readonly IServicioAlumnos _servicioAlumno;

    public EditarAlumnoViewModel(IServicioAlumnos servicioAlumno)
    {
        _servicioAlumno = servicioAlumno;
    }
}
