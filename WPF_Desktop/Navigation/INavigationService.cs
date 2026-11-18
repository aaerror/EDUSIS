using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_Desktop.Navigation;

internal interface INavigationService
{
	void Navigate();
}

/// <summary>
/// Navegación hacia <typeparamref name="TViewModel"/>. Es el tipo que declaran los constructores
/// de los ViewModels: el destino queda en la firma.
/// </summary>
/// <remarks>
/// Con todos los destinos inyectados como <see cref="INavigationService"/> a secas, una pantalla
/// con varios (Divisiones tiene cuatro, Cátedras tres) los distinguía SÓLO por posición: invertir
/// dos argumentos en <c>WPF_DesktopDI</c> compilaba y aterrizaba en la pantalla equivocada. Con el
/// destino en el tipo, ese error no compila.
/// </remarks>
internal interface INavigationService<out TViewModel> : INavigationService
	where TViewModel : ObservableObject
{
}
