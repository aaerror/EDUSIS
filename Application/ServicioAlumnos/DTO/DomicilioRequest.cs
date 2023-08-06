namespace Core.ServicioAlumnos.DTO;

public class DomicilioRequest
{
    public string Calle { get; set; }
    public int Altura { get; set; }
    public int Vivienda { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public string Localidad { get; set; }
    public string Provincia { get; set; }
    //public string CodigoPostal { get; private set; }
    public string Pais { get; set; } = "Argentina";
}
