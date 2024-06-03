namespace Core.ServicioMaterias.DTOs.Requests;

public record EliminarMateriaRequest
{
    public Guid CursoID { get; init; }
    public Guid MateriaID { get; init; }


    public EliminarMateriaRequest(Guid cursoID, Guid materiaID)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
    }
}
