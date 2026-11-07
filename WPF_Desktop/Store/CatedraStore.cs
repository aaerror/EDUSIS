using System;

namespace WPF_Desktop.Store;

/// <summary>
/// Cátedra elegida en la pantalla de cátedras, para que la de situación de revista sepa cuál es.
/// </summary>
/// <remarks>
/// Sigue siendo un <c>Guid</c> que nace en <c>Guid.Empty</c>: es el quinto centinela de la tanda
/// que UI-34 de <c>docs/todos.md</c> enumera, y no se migró a <c>Guid?</c> en este paso. El store
/// no notifica cambios a propósito: ver el hallazgo de los eventos muertos en <c>docs/todos.md</c>.
/// </remarks>
internal class CatedraStore : IEstadoDeSesion
{
	public Guid Catedra { get; set; } = Guid.Empty;

	public void Limpiar() =>
		Catedra = Guid.Empty;
}
