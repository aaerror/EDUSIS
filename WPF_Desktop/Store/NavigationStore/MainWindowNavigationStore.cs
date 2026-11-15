namespace WPF_Desktop.Store.NavigationStore;

/// <summary>
/// El ViewModel activo del área de contenido de <c>MainWindow</c>.
/// </summary>
/// <remarks>
/// Todo el mecanismo —incluida la liberación del scope de la pantalla saliente— vive en <see cref="StoreDeNavegacion"/>. Esta clase no agrega comportamiento: existe para
/// que el contenedor distinga los tres destinos por tipo, que es cómo
/// <c>NavigationService&lt;TViewModel, TStore&gt;</c> sabe a qué ventana navega.
/// </remarks>
internal class MainWindowNavigationStore : StoreDeNavegacion
{
}
