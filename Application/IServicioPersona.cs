using Domain.Personas;

namespace Core;

public interface IServicioPersona
{
    Persona BuscarPorDNI(string documento);
}
