namespace Core.ServicioAlumnos.DTO.Request;

public record DireccionRequest(string Calle,
                               int Altura,
                               int Vivienda,
                               string Observacion);
