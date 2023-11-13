namespace Core.ServicioAlumnos.DTOs.Requests;

public record DireccionRequest(string Calle,
                               string Altura,
                               int Vivienda,
                               string Observacion);
