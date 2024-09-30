using Domain.Materias.CargosDocentes;

namespace Core.ServicioMaterias.DTOs.Responses;

public record SituacionRevistaResponse(Guid DocenteID, string Docente, Cargo Cargo, DateTime FechaAlta, DateTime? FechaBaja, bool EnFunciones);