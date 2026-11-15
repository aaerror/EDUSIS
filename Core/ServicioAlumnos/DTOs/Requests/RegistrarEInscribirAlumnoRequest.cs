namespace Core.ServicioAlumnos.DTOs.Requests;

/// <summary>
/// Alta de un alumno junto con su primera inscripción. El alumno todavía no existe, así que la
/// inscripción no lleva su identificador: lo asigna el caso de uso.
/// </summary>
public record RegistrarEInscribirAlumnoRequest(
	RegistrarAlumnoRequest Alumno,
	Guid CursoID,
	Guid DivisionID,
	string Periodo,
	DateTime FechaInicio,
	bool EsRecursante = false);
