namespace WPF_Desktop.Store.NavigationStore;

/// <summary>
/// El ViewModel activo de la ventana modal, y si hay una abierta.
/// </summary>
/// <remarks>
/// El mecanismo de reemplazo y la liberación del scope —UI-18 y UI-49— viven en
/// <see cref="StoreDeNavegacion"/>. Lo propio de este store es que su vacío significa algo: un modal
/// sin ViewModel es un modal cerrado, que es lo que leen <c>MainViewModel.ModalEstaAbierto</c> y el
/// barrido de fin de sesión de <c>App</c>.
/// </remarks>
internal class ModalWindowNavigationStore : StoreDeNavegacion
{
	public bool EstaAbierto => TieneViewModel;

	/// <summary>
	/// Cierra el modal. Libera el ViewModel saliente y su scope por el camino de
	/// <see cref="StoreDeNavegacion.Reemplazar"/>, así que cerrar un modal ya no deja colgado el
	/// <c>DbContext</c> con el que se abrió.
	/// </summary>
	public void Cerrar() =>
		Reemplazar(null, null);
}
