namespace Core.Shared.DTOs.Cursadas;

/// <summary>
/// Una inscripción del historial del alumno. <c>FechaFin</c> nula y <c>EstaActiva</c> en
/// <c>true</c> describen la inscripción vigente; el resto son registro histórico, que es lo que
/// deja ver el recorrido de divisiones de un alumno trasladado.
/// </summary>
public record InscripcionHistoricaResponse(
	Guid CursanteID,
	Guid DivisionID,
	string Division,
	string Grado,
	string NivelEducativo,
	string CicloLectivo,
	DateTime FechaInicio,
	DateTime? FechaFin,
	bool EsRecursante,
	bool EstaActiva);
