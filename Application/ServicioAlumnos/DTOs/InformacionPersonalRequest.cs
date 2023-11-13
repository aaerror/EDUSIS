namespace Core.ServicioAlumnos.DTOs;

public record InformacionPersonalRequest(string Apellido,
                                         string Nombre,
                                         string Documento,
                                         int Sexo,
                                         DateTime FechaNacimiento,
                                         string Nacionalidad);