namespace Core.ServicioAlumnos.DTOs.Requests;

public record DomicilioRequest(string Calle,
                               string Altura,
                               string Vivienda,
                               string Observacion,
                               string Localidad,
                               string Provincia,
                               string Pais);
