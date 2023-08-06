using Core.ServicioAlumnos.DTO;

namespace Core.ServicioAlumnos;

public interface IServicioAlumno : IServicioPersona
{
    void RegistrarAlumno(InformacionPersonalRequest informacionPersonalRequest, DomicilioRequest domicilioRequest, ContactoRequest? contactoRequest);
    void AgregarContacto(ContactoRequest contactoRequest);
}
