namespace Core.ServicioCatedras.DTOs.Responses;

public record CatedraResponse(Guid CatedraID, Guid MateriaID, Guid DivisionID, int CargaHoraria, int HorasAsignadas, int HorasSinAsignar, Guid? DocenteEnFuncionesID);
