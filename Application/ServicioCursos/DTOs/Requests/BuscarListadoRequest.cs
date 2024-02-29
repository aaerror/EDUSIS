namespace Core.ServicioCursos.DTOs.Requests;

public record BuscarListadoRequest
{
    public Guid CursoID { get; init; }
    public Guid DivisionID { get; init; }
    public string Periodo { get; init; }


    public BuscarListadoRequest(Guid cursoID, Guid divisionID, string periodo)
    {
        CursoID = cursoID;
        DivisionID = divisionID;
        Periodo = periodo;
    }
}
