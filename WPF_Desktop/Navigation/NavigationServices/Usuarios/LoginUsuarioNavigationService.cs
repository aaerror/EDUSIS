using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.Navigation.NavigationServices.Usuarios;

internal class LoginUsuarioNavigationService<TViewModel> : INavigationService
	where TViewModel : ObservableObject
{
	private Func<TViewModel> _viewModelFactory;
	private StartupWindowNavigationStore _navigationStore;


	public LoginUsuarioNavigationService(Func<TViewModel> viewModelFactory, StartupWindowNavigationStore navigationStore)
	{
		_viewModelFactory = viewModelFactory;
		_navigationStore = navigationStore;
	}

	public void Navigate()
	{
		_navigationStore.ViewModel = _viewModelFactory();
	}
}