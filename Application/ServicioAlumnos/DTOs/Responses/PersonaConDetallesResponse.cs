namespace Core.ServicioAlumnos.DTOs.Responses;

public record PersonaConDetallesResponse
{
    public Guid PersonaId { get; set; }
    public string Apellido { get; set; }
    public string Nombre { get; set; }
    public string Documento { get; set; }
    public int Sexo { get; set; }
    public string FechaNacimiento { get; set; }
    public string Nacionalidad { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
    public string Calle { get; set; }
    public string Altura { get; set; }
    public int Vivienda { get; set; }
    public string Observacion { get; set; }
    public string Localidad { get; set; }
    public string Provincia { get; set; }
    public string Pais { get; set; }
}