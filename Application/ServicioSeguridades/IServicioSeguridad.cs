namespace Core.ServicioSeguridades;

public interface IServicioSeguridad
{
    public string HashPassword(string password);

    public bool ValidatePassword(string password, string salt, string hash);
}