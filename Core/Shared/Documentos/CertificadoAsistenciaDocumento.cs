namespace Core.Shared.Documentos;

/// <summary>
/// Acredita la concurrencia del estudiante durante un período.
/// </summary>
/// <remarks>
/// No lleva cómputo de presentes ni porcentaje: certifica que el estudiante asistió durante el
/// período, que es lo que el trámite requiere. El detalle cuantitativo de asistencia es materia
/// del reporte de asistencias, no de un certificado.
/// </remarks>
public sealed record CertificadoAsistenciaDocumento(
	IdentificacionAlumno Alumno,
	string CicloLectivo,
	RangoPeriodo Periodo,
	MetadatosEmision Emision);
