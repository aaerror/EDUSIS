using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioProfesores.DTOs.Responses;

namespace Core.ServicioProfesores
{
    public interface IServicioProfesores
    {
        ProfesorResponse BuscarProfesorPorDNI(string documento);
    }
}