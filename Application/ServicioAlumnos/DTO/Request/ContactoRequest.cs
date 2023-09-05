namespace Core.ServicioAlumnos.DTO.Request;

public record ContactoRequest
{
    public string Email { get; set; }
    public string Telefono { get; set; }
}
