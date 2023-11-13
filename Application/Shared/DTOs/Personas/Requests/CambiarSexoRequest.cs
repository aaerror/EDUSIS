namespace Core.Shared.DTOs.Personas.Requests;

public record CambiarSexoRequest
{
    public Guid PersonaID { get; set; }
    public string Apellido { get; init; }
    public string Nombre { get; init; }
    public int Sexo { get; init; }


    public CambiarSexoRequest(Guid personaID, string apellido, string nombre, int sexo)
    {
        PersonaID = personaID;
        Apellido = apellido;
        Nombre = nombre;
        Sexo = sexo;
    }
}
