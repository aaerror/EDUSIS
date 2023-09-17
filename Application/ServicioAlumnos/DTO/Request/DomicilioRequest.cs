namespace Core.ServicioAlumnos.DTO.Request;

public record DomicilioRequest(string Calle,
                               int Altura,
                               int Vivienda,
                               string Observacion,
                               string Localidad,
                               string Provincia,
                               string Pais);
