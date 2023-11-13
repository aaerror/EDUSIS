namespace Core.ServicioCursos.DTOs.Requests;

public record CrearDocenteEnFuncionesRequest
{
    public Guid CursoID { get; init; }
    public Guid MateriaID { get; init; }
    public Guid DocenteID { get; init; }


    public CrearDocenteEnFuncionesRequest(Guid cursoID, Guid materiaID, Guid docenteID)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
        DocenteID = docenteID;
    }
}
