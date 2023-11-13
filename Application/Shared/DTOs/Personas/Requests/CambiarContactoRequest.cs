namespace Core.Shared.DTOs.Personas.Requests;

public record CambiarContactoRequest
{
    public Guid PersonaID { get; init; }
    public string Telefono { get; init; }
    public string Email { get; init; }


    public CambiarContactoRequest(Guid personaID, string telefono, string email)
    {
        PersonaID = personaID;
        Telefono = telefono;
        Email = email;
    }
}
