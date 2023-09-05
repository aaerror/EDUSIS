namespace Core.ServicioAlumnos.DTO.Response;

public record PersonaResponse
{
    public Guid PersonaId { get; init; }
    public string NombreCompleto { get; init; }
    public string Documento { get; init; }
    public string Sexo { get; init; }
    public DateTime FechaNacimiento { get; init; }
    public string Nacionalidad { get; init; }
}
