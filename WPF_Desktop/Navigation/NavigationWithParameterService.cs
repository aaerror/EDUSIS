using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.Navigation;

internal class NavigationWithParameterService<TParameter, TViewModel>
	where TViewModel : ObservableObject
{
	private Func<TParameter, TViewModel> _viewModelFactory;
	private readonly MainWindowNavigationStore _navigationStore;


	public NavigationWithParameterService(Func<TParameter, TViewModel> viewModelFactory, MainWindowNavigationStore navigationStore)
	{
		_viewModelFactory = viewModelFactory;
		_navigationStore = navigationStore;
	}

	public void Navigation(TParameter parameter) =>
		_navigationStore.ViewModel = _viewModelFactory(parameter);
}