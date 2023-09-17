using System;

namespace WPF_Desktop.Models;

public class AlumnoDTO
{
    public Guid PersonaId { get; set; }
    public string Apellido { get; set; }
    public string Nombre { get; set; }
    public string Documento { get; set; }
    public int Sexo { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Nacionalidad { get; set; }
    public DomicilioDTO Domicilio { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
}
