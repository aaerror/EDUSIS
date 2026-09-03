namespace Core.ServicioCursos.DTOs.Requests;

public record EliminarDivisionRequest
{
    public Guid CursoID { get; init; }
    public Guid DivisionID {  get; init; }


    public EliminarDivisionRequest(Guid cursoID, Guid divisionID)
    {
        CursoID = cursoID;
        DivisionID = divisionID;
    }
}
