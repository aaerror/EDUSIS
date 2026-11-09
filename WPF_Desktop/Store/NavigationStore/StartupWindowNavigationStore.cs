namespace WPF_Desktop.Store.NavigationStore;

/// <summary>
/// El ViewModel activo del área de contenido de <c>StartupWindow</c>: acceso y registro de cuenta.
/// </summary>
/// <remarks>
/// Todo el mecanismo —incluida la liberación del scope de la pantalla saliente, que es UI-18 y
/// UI-49— vive en <see cref="StoreDeNavegacion"/>. Esta clase no agrega comportamiento: existe para
/// que el contenedor distinga los tres destinos por tipo.
/// </remarks>
internal class StartupWindowNavigationStore : StoreDeNavegacion
{
}
