namespace Core.ServicioMaterias.DTOs.Requests;

public record RegistrarDocenteEnFuncionesRequest
{
    public Guid CursoID { get; init; }
    public Guid MateriaID { get; init; }
    public Guid DocenteID { get; init; }


    public RegistrarDocenteEnFuncionesRequest(Guid cursoID, Guid materiaID, Guid docenteID)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
        DocenteID = docenteID;
    }
}
