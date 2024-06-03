namespace Core.ServicioMaterias.DTOs.Requests;

public record NombreDuplicadoRequest
{
    public Guid CursoID { get; init; }
    public string Descripcion { get; init; }


    public NombreDuplicadoRequest(Guid cursoID, string descripcion)
    {
        CursoID = cursoID;
        Descripcion = descripcion;
    }
}