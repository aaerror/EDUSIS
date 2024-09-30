using Domain.Materias.CargosDocentes;

namespace Core.ServicioMaterias.DTOs.Requests;

public record RegistrarDocenteEnMateriaRequest(Guid CursoID, Guid MateriaID, Guid DocenteID, Cargo Cargo, DateTime FechaAlta, bool EnFunciones);