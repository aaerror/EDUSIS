namespace Core.ServicioMaterias.DTOs.Requests;

public record BuscarMateriaSegunCursoRequest
{
    public Guid CursoID { get; init; }


    public BuscarMateriaSegunCursoRequest(Guid cursoID)
    {
        CursoID = cursoID;
    }
}
