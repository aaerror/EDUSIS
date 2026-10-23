namespace Core.Shared.Documentos;

/// <summary>
/// Una materia en el boletín: la nota de cada instancia y la condición resultante.
/// </summary>
/// <remarks>
/// Las notas son texto y no <c>double?</c> a propósito: la celda puede no ser un número —"—"
/// cuando no hay nota registrada, "No rindió" cuando se registró inasistencia al examen—, y
/// decidir eso es parte de armar el boletín, no de imprimirlo.
/// </remarks>
public sealed record FilaBoletin(string Materia, string Parcial, string Recuperatorio, string Condicion);

/// <summary>
/// Boletín de calificaciones del estudiante para un ciclo lectivo: una fila por materia de la
/// currícula, con la nota de cada instancia y la condición.
/// </summary>
public sealed record BoletinCalificacionesDocumento(
	IdentificacionAlumno Alumno,
	string CicloLectivo,
	IReadOnlyCollection<FilaBoletin> Filas,
	MetadatosEmision Emision);
