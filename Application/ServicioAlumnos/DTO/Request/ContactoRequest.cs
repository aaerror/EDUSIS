using System.ComponentModel.DataAnnotations;

namespace Core.ServicioAlumnos.DTO.Request;

public record ContactoRequest(string Email, string Telefono);
