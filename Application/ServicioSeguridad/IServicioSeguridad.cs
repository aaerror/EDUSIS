namespace Core.ServicioSeguridad;

public interface IServicioSeguridad
{
    public string HashPassword(string password);

    public bool ValidatePassword(string password, string salt, string hash);
}