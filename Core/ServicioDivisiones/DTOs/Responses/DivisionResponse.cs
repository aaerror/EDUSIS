namespace Core.ServicioDivisiones.DTOs.Responses;

public record DivisionResponse(Guid DivisionID, string Descripcion, Guid? PreceptorID, string? Preceptor, int Cursantes);