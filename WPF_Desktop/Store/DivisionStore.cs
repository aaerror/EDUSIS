using System;
namespace WPF_Desktop.Store;

/// <summary>
/// División elegida en la pantalla de divisiones. Sólo el identificador: es lo único que lee la
/// pantalla de cursantes, el único destino de esta navegación.
/// </summary>
/// <remarks>
/// No guarda un <c>DivisionViewModel</c> (violación 14 de <c>docs/todos.md</c>) ni el
/// <c>DivisionResponse</c> completo: <c>PreceptorID</c>, <c>Preceptor</c> y <c>Cursantes</c> no los
/// lee ningún destino y en un Singleton que nadie limpia quedarían obsoletos en cuanto alguien
/// cambie el preceptor de la división. Es un record y no un <c>Guid?</c> para que los tres stores
/// de este drill-down se lean igual y para poder sumarle contexto sin tocar a los consumidores.
/// </remarks>
internal sealed record DivisionSeleccionada(Guid DivisionID);

internal class DivisionStore : IEstadoDeSesion
{
	public DivisionSeleccionada Division { get; set; }

	public void Limpiar() =>
		Division = null!;
}
