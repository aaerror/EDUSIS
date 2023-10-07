using System.ComponentModel.DataAnnotations;

namespace Core.ServicioAlumnos.DTOs.Requests;

public record ContactoRequest(string Email, string Telefono);
