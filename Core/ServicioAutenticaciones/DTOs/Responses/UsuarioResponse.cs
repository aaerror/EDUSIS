namespace Core.ServicioAutenticaciones.DTOs.Responses;

public record UsuarioResponse(Guid UsuarioID, Guid DocenteID, string Usuario, string NombreCompleto, string Puesto, List<string> Roles);