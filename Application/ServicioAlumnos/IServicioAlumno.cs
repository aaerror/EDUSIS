using Core.ServicioAlumnos.DTO.Request;
using Core.ServicioAlumnos.DTO.Response;

namespace Core.ServicioAlumnos;

public interface IServicioAlumno
{
    void RegistrarAlumno(InformacionPersonalRequest informacionPersonalRequest, DomicilioRequest domicilioRequest, ContactoRequest contactoRequest);

    PersonaResponse BuscarPorDNI(string documento);

    bool EsDocumentoValido(string documento);

    void ModificarSexo(Guid personaId, CambiarSexoRequest cambiarSexoRequest);

    void ModificarNombreCompleto(Guid personaId, string nuevoApellido, string nuevoNombre);

    void ActualizarDomicilio(Guid personaId, DomicilioRequest domicilioRequest);

    void ActualizarDireccion(Guid personaId, DireccionRequest domicilioRequest);
}
