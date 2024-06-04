namespace Core.ServicioMaterias.DTOs.Requests;

public record BuscarHorariosRequest
{
    public Guid CursoID { get; init; }
    public Guid MateriaID { get; init; }


    public BuscarHorariosRequest(Guid cursoID, Guid materiaID)
    {
        CursoID = cursoID;
        MateriaID = materiaID;
    }
}