using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioCusos.DTOs.Responses;

namespace Core.ServicioCursos;

public interface IServicioCurso
{
    IReadOnlyCollection<CursoResponse> ListarCursos();
    Task RegistrarCurso(RegistrarCursoRequest request);
    void EliminarCurso(EliminarCursoRequest request);

    #region Divisiones
    IReadOnlyCollection<DivisionResponse> BuscarDivisiones(Guid unCurso);
    IReadOnlyCollection<CursanteResponse> BuscarListado(BuscarListadoRequest request);
    void InscribirAlumnoEnDivision(CrearCursanteRequest request);
    void AgregarDivisionAlCurso(Guid unCurso);
    void QuitarDivisiosDelCurso(EliminarDivisionRequest request);
    #endregion

    #region Calificación
    void RegistrarCalificacion(CrearCalificationRequest request);
    #endregion
}