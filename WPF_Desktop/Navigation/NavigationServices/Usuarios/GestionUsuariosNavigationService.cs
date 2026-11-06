using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.Navigation.NavigationServices.Usuarios;

/// <summary>
/// Navega a la raíz de la sección «Usuarios», gobernada por <c>AdministrarUsuarios</c>. Es la
/// séptima raíz del menú y la que cierra UI-46: hasta el 2026-11-06 ese permiso —junto con
/// <c>AsignarRoles</c>— no habilitaba ninguna entrada, así que los dos casos de uso de roles
/// existían sin un camino de usuario que los alcanzara.
/// </summary>
internal class GestionUsuariosNavigationService<TViewModel> : INavigationService
	where TViewModel : ObservableObject
{
	private Func<TViewModel> _viewModelFactory;
	private MainWindowNavigationStore _navigationStore;


	public GestionUsuariosNavigationService(Func<TViewModel> viewModelFactory, MainWindowNavigationStore navigationStore)
	{
		_viewModelFactory = viewModelFactory;
		_navigationStore = navigationStore;
	}

	public void Navigate()
	{
		_navigationStore.ViewModel = _viewModelFactory();
	}
}
