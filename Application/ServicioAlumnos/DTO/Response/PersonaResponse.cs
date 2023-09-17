namespace Core.ServicioAlumnos.DTO.Response;

public record PersonaResponse(Guid PersonaId,
                              string Apellido,
                              string Nombre,
                              string Documento,
                              int Sexo,
                              string FechaNacimiento,
                              string Nacionalidad);
