namespace Core.ServicioCursos.DTOs.Requests;

public record EliminarSituacionRevistaRequest
{
    public Guid CursoID { get; init; }
    public Guid MateriaID { get; init; }
    public Guid DocenteID { get; init; }
    public DateTime FechaBaja { get; init; }


    public EliminarSituacionRevistaRequest(Guid cursoID, Guid materiaID, Guid docenteID, DateTime fechaBaja)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
        DocenteID = docenteID;
        FechaBaja = fechaBaja;
    }
}
