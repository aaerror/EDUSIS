namespace Core.ServicioMaterias.DTOs.Responses;

public record SituacionRevistaResponse(Guid DocenteID, string Docente, int Cargo, string CargoDescripcion, DateTime FechaAlta, DateTime? FechaBaja, bool EnFunciones);