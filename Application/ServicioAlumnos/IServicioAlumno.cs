﻿using Core.ServicioAlumnos.DTO.Request;
using Core.ServicioAlumnos.DTO.Response;

namespace Core.ServicioAlumnos;

public interface IServicioAlumno
{
    void RegistrarAlumno(InformacionPersonalRequest informacionPersonalRequest,
                         DomicilioRequest domicilioRequest,
                         ContactoRequest contactoRequest);

    PersonaResponse BuscarPorDNI(string documento);

    PersonaConDetallesResponse BuscarPorDNIConDetalles(string documento);

    bool EsDocumentoValido(string documento);

    void ModificarContacto(Guid personaId, ContactoRequest cambioContactoRequest);

    void ModificarSexo(Guid personaId, CambiarSexoRequest cambioSexoRequest);

    void ModificarNombreCompleto(Guid personaId, string nuevoApellido, string nuevoNombre);

    void ModificarDomicilio(Guid personaId, DomicilioRequest domicilioRequest);

    void ActualizarDireccion(Guid personaId, DireccionRequest domicilioRequest);
}
