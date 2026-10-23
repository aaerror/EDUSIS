namespace Core.Shared.Documentos;

/// <summary>
/// Acredita que el estudiante cursa regularmente en el ciclo lectivo indicado.
/// </summary>
public sealed record CertificadoAlumnoRegularDocumento(
	IdentificacionAlumno Alumno,
	string CicloLectivo,
	DateTime FechaIngreso,
	MetadatosEmision Emision);
