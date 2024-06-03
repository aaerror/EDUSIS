namespace Core.ServicioAutenticaciones.DTOs.Request;

public record LoginRequest
{
    public string Usuario { get; init; }
    public string Clave { get; init; }


    public LoginRequest(string usuario, string clave)
    {
        Usuario = usuario;
        Clave = clave;
    }
}
