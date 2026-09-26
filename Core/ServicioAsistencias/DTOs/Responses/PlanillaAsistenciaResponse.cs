namespace Core.ServicioAsistencias.DTOs.Responses;

public record PlanillaAsistenciaResponse(
	Guid PlanillaID,
	Guid DivisionID,
	DateTime Fecha,
	Guid PreceptorID,
	bool Cerrada,
	IReadOnlyCollection<RegistroAsistenciaResponse> Registros,
	int Presentes,
	int Ausencias,
	int Inasistencias,
	int Tardanzas);
