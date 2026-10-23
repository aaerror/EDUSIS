namespace Core.Shared.Documentos;

/// <summary>Qué informa una constancia de materias.</summary>
public enum TipoConstanciaMaterias
{
	Aprobadas,
	Pendientes
}

/// <summary>Una materia de la currícula tal como se informa en la constancia.</summary>
public sealed record MateriaInformada(string Materia, int HorasCatedra, string Condicion);

/// <summary>
/// Informa las materias aprobadas o pendientes del estudiante en el ciclo lectivo indicado.
/// </summary>
/// <remarks>
/// Un solo tipo para los dos documentos: comparten encabezado, tabla y pie, y lo único que
/// cambia es el conjunto de materias que se lista y el título. Separarlos en dos records
/// idénticos sólo duplicaría la plantilla.
/// </remarks>
public sealed record ConstanciaMateriasDocumento(
	IdentificacionAlumno Alumno,
	string CicloLectivo,
	TipoConstanciaMaterias Tipo,
	IReadOnlyCollection<MateriaInformada> Materias,
	MetadatosEmision Emision);
