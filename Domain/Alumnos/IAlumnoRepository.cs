using Domain.Personas;

namespace Domain.Alumnos;

public interface IAlumnoRepository: IPersonaRepository<Alumno>
{
    public bool EsLegajoValido(string legajo);

    Task<Alumno?> BuscarPorNombreCompletoAsync(string nombreCompleto);
    Task<Alumno?> BuscarPorDocumentoAsync(string documento);
}
