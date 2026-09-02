using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.Navigation.NavigationServices;

internal class VerPerfilNavigationService<TViewModel> : INavigationService
	where TViewModel : ObservableObject
{
	private readonly Func<TViewModel> _viewModelFactory;
	private readonly MainWindowNavigationStore _navigationStore;


	public VerPerfilNavigationService(Func<TViewModel> viewModelFactory, MainWindowNavigationStore navigationStore)
	{
		_viewModelFactory = viewModelFactory;
		_navigationStore = navigationStore;
	}

	public void Navigate() =>
		_navigationStore.ViewModel = _viewModelFactory();
}