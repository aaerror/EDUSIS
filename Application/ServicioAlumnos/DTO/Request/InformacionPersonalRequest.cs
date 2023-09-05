namespace Core.ServicioAlumnos.DTO.Request;

public record InformacionPersonalRequest
{
    public string Apellido { get; set; }
    public string Nombre { get; set; }
    public string Documento { get; set; }
    public int Sexo { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Nacionalidad { get; set; }
}
