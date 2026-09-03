namespace Core.ServicioDocentes.DTOs.Responses;

public record DocenteResponse
{
    public Guid DocenteId { get; init; }
    public string NombreCompleto { get; init; }
    public string Documento { get; init; }
    public string Legajo { get; init; }
    public DateTime FechaAlta { get; init; }
}
