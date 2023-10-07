namespace Core.ServicioAlumnos.DTOs.Responses;

public record PersonaResponse(Guid PersonaId,
                              string Apellido,
                              string Nombre,
                              string Documento,
                              int Sexo,
                              string FechaNacimiento,
                              string Nacionalidad);
