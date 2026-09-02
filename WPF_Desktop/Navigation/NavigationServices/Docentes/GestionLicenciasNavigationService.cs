using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Store.NavigationStore;

internal class GestionLicenciasNavigationService<TViewModel> : INavigationService
	where TViewModel : ObservableObject
{
	private Func<TViewModel> _viewModelFactory;
	private MainWindowNavigationStore _navigationStore;


	public GestionLicenciasNavigationService(Func<TViewModel> viewModelFactory, MainWindowNavigationStore navigationStore)
	{
		_viewModelFactory = viewModelFactory;
		_navigationStore = navigationStore;
	}

	public void Navigate()
	{
		_navigationStore.ViewModel = _viewModelFactory();
	}
}