namespace Core.Shared.Documentos;

/// <summary>
/// Bloque de identificación del estudiante y su trayectoria, común a los seis documentos.
/// </summary>
/// <remarks>
/// Está en un solo lugar porque los seis documentos tienen que identificar al mismo estudiante
/// con los mismos datos: si cada certificado armara su propio juego de campos, dos documentos
/// del mismo alumno podrían nombrarlo distinto. <see cref="Division"/> es opcional porque la
/// trayectoria puede informarse a nivel de curso cuando la división no corresponde.
/// </remarks>
public sealed record IdentificacionAlumno(
	string NombreCompleto,
	string Documento,
	string Legajo,
	string NivelEducativo,
	string Curso,
	string? Division);
