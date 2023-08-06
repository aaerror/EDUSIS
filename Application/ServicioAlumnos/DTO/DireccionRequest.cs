namespace Core.ServicioAlumnos.DTO;

public class DireccionRequest
{
    public string Calle { get; set; }
    public int Altura { get; set; }
    public int Vivienda { get; set; }
    public string Observacion { get; set; } = string.Empty;
}
