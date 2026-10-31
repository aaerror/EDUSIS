using System.Net;
using System.Security;

namespace Core.ServicioSeguridad;

internal interface IServicioSeguridad
{
    public Dictionary<string, string> HashPassword(string password);

    public bool ValidatePassword(SecureString password, string salt, string hash);
}