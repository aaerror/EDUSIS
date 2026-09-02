using Core.Shared.DTOs.Personas.Requests;

namespace Core.ServicioDocentes.DTOs.Requests;

public record RegistrarDocenteRequest(string Legajo, string CUIL, DateTime FechaAlta, RegistrarDatosPersonalesRequest DatosPersonales, RegistrarDomicilioRequest Domicilio, RegistrarContactoRequest Contacto, RegistrarPuestoDocenteRequest Puesto);
