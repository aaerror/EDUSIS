namespace Core.ServicioDocentes.DTOs.Responses;

public record ProfesorResponse
{
    public Guid ProfesorId { get; init; }
    public string NombreCompleto { get; init; }
    public string Documento { get; init; }
    public string Legajo { get; init; }


    public ProfesorResponse(Guid profesorId, string nombreCompleto, string documento, string legajo)
    {
        ProfesorId = profesorId;
        NombreCompleto = nombreCompleto;
        Documento = documento;
        Legajo = legajo;
    }
}
