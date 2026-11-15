using System;

namespace WPF_Desktop.Store;

/// <summary>
/// Cátedra elegida en la pantalla de cátedras, para que la de situación de revista sepa cuál es.
/// </summary>
/// <remarks>
/// Es un <c>Guid</c> que nace en <c>Guid.Empty</c> (un centinela que el compilador no distingue de
/// un valor real); lo correcto sería <c>Guid?</c>, como en <c>LegajoStore</c>. El store no notifica
/// cambios a propósito: los ViewModels son <c>Transient</c> y lo leen en el constructor.
/// </remarks>
internal class CatedraStore : IEstadoDeSesion
{
	public Guid Catedra { get; set; } = Guid.Empty;

	public void Limpiar() =>
		Catedra = Guid.Empty;
}
