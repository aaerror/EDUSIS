namespace Core.ServicioAlumnos.DTO.Request;

public record DireccionRequest
{
    public string Calle { get; set; }
    public int Altura { get; set; }
    public int Vivienda { get; set; }
    public string Observacion { get; set; } = string.Empty;
}
