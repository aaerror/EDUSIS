namespace Core.ServicioCursos.DTOs.Requests;

public record CrearCursanteRequest
{
    public Guid CursoID { get; init; }
    public Guid DivisionID { get; init; }
    public Guid AlumnoID { get; init; }
    public string Periodo { get; init; }


    public CrearCursanteRequest(Guid cursoID, Guid divisionID, Guid alumnoID, string periodo)
    {
        CursoID = cursoID;
        DivisionID = divisionID;
        AlumnoID = alumnoID;
        Periodo = periodo;
    }
}
