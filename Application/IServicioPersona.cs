using Core.ServicioAlumnos.DTO;

namespace Core;

public interface IServicioPersona
{
    bool EsDocumentoValido(string documento);

    void CambiarDomicilio(Guid personaId, DomicilioRequest request);

    void CambiarDireccion(Guid personaId, DireccionRequest request);
}
