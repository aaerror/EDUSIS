namespace Core.Shared.Documentos;

/// <summary>
/// Una materia en el boletín: la nota que quedó de cada período y la condición resultante.
/// </summary>
/// <remarks>
/// <para>
/// Las notas son texto y no <c>decimal?</c> a propósito: la celda puede no ser un número —"—"
/// cuando el período todavía no se tomó, "No rindió" cuando se registró inasistencia al examen—, y
/// decidir eso es parte de armar el boletín, no de imprimirlo.
/// </para>
/// <para>
/// Cada cuatrimestre es UNA columna aunque admita recuperatorio: lo que se informa es la nota
/// vigente del período, que es la del recuperatorio cuando lo hubo. Las dos recuperaciones también
/// comparten columna, porque a la segunda se llega sólo después de no aprobar la primera y nunca
/// interesan las dos a la vez. Reemplazó a un par de columnas <c>Parcial</c>/<c>Recuperatorio</c>
/// que venían del modelo de instancias anterior, donde "recuperatorio" no decía de qué.
/// </para>
/// </remarks>
public sealed record FilaBoletin(
	string Materia,
	string PrimerCuatrimestre,
	string SegundoCuatrimestre,
	string Final,
	string Recuperacion,
	string Condicion);

/// <summary>
/// Boletín de calificaciones del estudiante para un ciclo lectivo: una fila por materia de la
/// currícula, con la nota de cada período y la condición.
/// </summary>
public sealed record BoletinCalificacionesDocumento(
	IdentificacionAlumno Alumno,
	string CicloLectivo,
	IReadOnlyCollection<FilaBoletin> Filas,
	MetadatosEmision Emision);
