using System;

namespace WPF_Desktop.Store;

/// <summary>
/// Persona elegida en la pantalla de alumnos o de docentes, para que la pantalla de perfil y la de
/// inscripción sepan de quién se trata.
/// </summary>
/// <remarks>
/// <c>PersonaID</c> es <c>Guid?</c> y no <c>Guid</c>: tipado como <c>Guid</c> nacía en
/// <c>Guid.Empty</c>, un valor que significa lo mismo que <c>null</c> pero que el compilador no
/// distingue, así que "nadie eligió una persona" era indistinguible de una persona real y el
/// request salía igual hacia <c>Core</c>. Es UI-34 de <c>docs/todos.md</c>; la guarda que lo
/// acompaña vive en cada ViewModel que lee el store.
/// </remarks>
internal class LegajoStore : IEstadoDeSesion
{
	public Guid? PersonaID { get; set; }

	public string Documento { get; set; }

	/// <summary>
	/// El más importante de los barridos de <see cref="IEstadoDeSesion"/>: acá queda el alumno o el
	/// docente que el usuario anterior estaba mirando, y es un dato personal de un tercero.
	/// </summary>
	public void Limpiar()
	{
		PersonaID = null;
		Documento = string.Empty;
	}
}
