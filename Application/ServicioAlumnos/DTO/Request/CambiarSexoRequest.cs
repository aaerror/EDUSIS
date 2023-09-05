namespace Core.ServicioAlumnos.DTO.Request;

public record CambiarSexoRequest
{
    public string Apellido { get; init; }
    public string Nombre { get; init; }
    public int Sexo { get; init; }
}
