namespace Core.ServicioAlumnos.DTO.Response;

public record PersonaConDetallesResponse(Guid PersonaId,
                                         string Apellido,
                                         string Nombre,
                                         string Documento,
                                         int Sexo,
                                         string FechaNacimiento,
                                         string Nacionalidad,
                                         string Email,
                                         string Telefono,
                                         string Calle,
                                         int Altura,
                                         int Vivienda,
                                         string Observacion,
                                         string Localidad,
                                         string Provincia,
                                         string Pais);
