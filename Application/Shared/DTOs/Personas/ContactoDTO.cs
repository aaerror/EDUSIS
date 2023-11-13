namespace Core.Shared.DTOs.Personas;

public record ContactoDTO
{
    public string Telefono { get; init; }
    public string Email { get; init; }


    public ContactoDTO(string telefono, string email)
    {
        Telefono = telefono;
        Email = email;
    }
}
