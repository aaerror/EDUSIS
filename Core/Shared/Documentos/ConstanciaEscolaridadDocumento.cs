namespace Core.Shared.Documentos;

/// <summary>
/// Acredita la escolaridad del estudiante en el establecimiento: nivel, curso y división del
/// ciclo lectivo indicado, más la fecha desde la que está escolarizado.
/// </summary>
public sealed record ConstanciaEscolaridadDocumento(
	IdentificacionAlumno Alumno,
	string CicloLectivo,
	DateTime FechaIngreso,
	MetadatosEmision Emision);
