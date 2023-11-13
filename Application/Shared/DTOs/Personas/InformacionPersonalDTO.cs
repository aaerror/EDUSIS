namespace Core.Shared.DTOs.Personas;

public record InformacionPersonalDTO
{
    public string Apellido { get; set; }
    public string Nombre { get; set; }
    public string Documento { get; set; }
    public int Sexo { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Nacionalidad { get; set; }


    public InformacionPersonalDTO(string apellido, string nombre, string documento, int sexo, DateTime fechaNacimiento, string nacionalidad)
    {
        Apellido = apellido;
        Nombre = nombre;
        Documento = documento;
        Sexo = sexo;
        FechaNacimiento = fechaNacimiento;
        Nacionalidad = nacionalidad;
    }
}
