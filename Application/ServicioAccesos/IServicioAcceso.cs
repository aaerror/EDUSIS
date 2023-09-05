namespace Core.ServicioAccesos;

public interface IServicioAcceso
{
    bool EstaLogeado();
    bool AutenticarUsuario(string email, string password);
    void CambiarPassword(string password);
}
