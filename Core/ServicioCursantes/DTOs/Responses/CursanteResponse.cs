namespace Core.ServicioCursantes.DTOs.Responses;

public record CursanteResponse(Guid CursanteID, Guid AlumnoID, string NombreCompleto, string Documento, int Edad, bool EsRecursante);
