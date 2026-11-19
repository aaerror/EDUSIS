using System;

namespace WPF_Desktop.Store;

/// <summary>
/// Cátedra elegida en la pantalla de cátedras, para que las de horarios y situación de revista sepan
/// cuál es.
/// </summary>
/// <remarks>
/// <c>Catedra</c> es <c>Guid?</c> y no <c>Guid</c>, como <c>LegajoStore.PersonaID</c>: tipado como
/// <c>Guid</c> nacía en <c>Guid.Empty</c>, un centinela que el compilador no distingue de un valor
/// real, y un destino abierto sin cátedra elegida mandaba ese valor a <c>Core</c>. La guarda vive en
/// cada ViewModel que lee el store. El store no notifica cambios a propósito: los ViewModels son
/// <c>Transient</c> y lo leen al usarlo.
/// </remarks>
internal class CatedraStore : IEstadoDeSesion
{
	public Guid? Catedra { get; set; }

	public void Limpiar() =>
		Catedra = null;
}
