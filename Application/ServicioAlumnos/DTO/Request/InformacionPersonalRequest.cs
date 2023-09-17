namespace Core.ServicioAlumnos.DTO.Request;

public record InformacionPersonalRequest(string Apellido,
                                         string Nombre,
                                         string Documento,
                                         int Sexo,
                                         DateTime FechaNacimiento,
                                         string Nacionalidad);