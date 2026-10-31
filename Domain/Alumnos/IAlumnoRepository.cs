using Domain.Personas;
using Domain.Shared;

namespace Domain.Alumnos;

public interface IAlumnoRepository : IPersonaRepository<Alumno>
{
	Task<bool> EsLegajoInvalidoAsync(string legajo);
	Task<string?> BuscarUltimoLegajoAsync();
	Task<Alumno?> BuscarPorNombreCompletoAsync(string nombreCompleto);
	Task<Alumno?> BuscarPorDocumentoAsync(string documento);
}
