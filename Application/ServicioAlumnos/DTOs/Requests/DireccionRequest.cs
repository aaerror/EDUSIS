namespace Core.ServicioAlumnos.DTOs.Requests;

public record DireccionRequest(string Calle,
                               int Altura,
                               int Vivienda,
                               string Observacion);
