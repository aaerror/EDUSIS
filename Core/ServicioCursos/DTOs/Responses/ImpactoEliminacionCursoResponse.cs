namespace Core.ServicioCursos.DTOs.Responses;

/// <summary>
/// Qué se destruye si se elimina el curso: sus currículas y las materias de cada una. Lo consume el
/// diálogo de confirmación.
/// </summary>
/// <remarks>
/// <para>
/// No informa inscripciones ni calificaciones, y no es un olvido: el curso <strong>no se puede
/// eliminar mientras le quede una división</strong> (<c>Curso.ValidarSePuedeEliminar</c>), y sin divisiones no hay
/// cursantes ni calificaciones. Si este diálogo llegara a mostrarse, ese material ya no existe.
/// </para>
/// <para>
/// <c>Catedras</c> sí se informa: las cátedras cuelgan de la materia, no de la división, así que una
/// materia puede conservar cátedras de divisiones ya eliminadas. Las limpia
/// <c>MateriaEliminadaEventHandler</c> cuando el borrado despacha <c>MateriaEliminadaEvent</c>.
/// </para>
/// </remarks>
public record ImpactoEliminacionCursoResponse(
	int Curriculas,
	int Materias,
	int Catedras,
	int Divisiones)
{
	/// <summary>
	/// El curso tiene divisiones y por lo tanto <strong>no se puede eliminar</strong>. La pantalla lo
	/// usa para mostrar el motivo del rechazo —y cuántas divisiones hay que eliminar antes— en lugar
	/// de ofrecer una confirmación que el caso de uso va a rechazar.
	/// </summary>
	public bool TieneDivisiones => Divisiones > 0;

	public bool TieneCurricula => Curriculas > 0 || Materias > 0;
}
