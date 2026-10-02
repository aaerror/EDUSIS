using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.Navigation.NavigationServices.Cursos;

internal class GestionCatedrasNavigationService<TViewModel> : INavigationService
	where TViewModel : ObservableObject
{
	private Func<TViewModel> _viewModelFactory;
	private MainWindowNavigationStore _navigationStore;


	public GestionCatedrasNavigationService(Func<TViewModel> viewModelFactory, MainWindowNavigationStore navigationStore)
	{
		_viewModelFactory = viewModelFactory;
		_navigationStore = navigationStore;
	}

	public void Navigate()
	{
		_navigationStore.ViewModel = _viewModelFactory();
	}
}
