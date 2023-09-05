using Core.Shared;

namespace Core.ServicioAccesos;

public class ServicioAcceso : IServicio, IServicioAcceso
{
    public ServicioAcceso()
    {

    }

    public bool AutenticarUsuario(string email, string password)
    {
        throw new NotImplementedException();
    }

    public void CambiarPassword(string password)
    {
        throw new NotImplementedException();
    }

    public bool EstaLogeado()
    {
        throw new NotImplementedException();
    }
}
