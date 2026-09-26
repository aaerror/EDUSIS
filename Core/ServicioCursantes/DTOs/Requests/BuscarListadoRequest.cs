namespace Core.ServicioCursantes.DTOs.Requests;

public record BuscarListadoRequest(Guid CursoID, Guid DivisionID, string Periodo);
