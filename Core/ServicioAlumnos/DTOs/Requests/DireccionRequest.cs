namespace Core.ServicioAlumnos.DTOs.Requests;

public record DireccionRequest(string Calle,
                               string Altura,
                               string Vivienda,
                               string Observacion);
