namespace Core.ServicioAlumnos.DTOs.Requests;

public record RegistrarAlumnoRequest(string Apellido, string Nombre, string DNI, string Sexo, DateTime FechaNacimiento, string Nacionalidad, string Telefono, string Email, string Calle, string Altura, string Vivienda, string Observacion, string Localidad, string Provincia, string Pais);