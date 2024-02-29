using Core.ServicioAlumnos.DTOs;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;

namespace Core.ServicioAlumnos;

public interface IServicioAlumnos
{
    Guid RegistrarAlumno(RegistrarAlumnoRequest request);

    PersonaResponse BuscarPorDNI(string documento);

    PersonaConDetallesResponse BuscarPorDNIConDetalles(string documento);

    bool EsDocumentoInvalido(string documento);

    void ModificarContacto(Guid personaId, ContactoRequest cambioContactoRequest);

    void ModificarSexo(Guid personaId, CambiarSexoRequest cambioSexoRequest);

    void ModificarNombreCompleto(Guid personaId, string nuevoApellido, string nuevoNombre);

    void ModificarDomicilio(Guid personaId, DomicilioRequest domicilioRequest);

    void ActualizarDireccion(Guid personaId, DireccionRequest domicilioRequest);

    void QuitarAlumno(Guid alumnoId);
}
