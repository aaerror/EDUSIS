using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.Navigation.NavigationServices.Reportes;

internal class GestionReportesNavigationService<TViewModel> : INavigationService
	where TViewModel : ObservableObject
{
	private Func<TViewModel> _viewModelFactory;
	private MainWindowNavigationStore _navigationStore;


	public GestionReportesNavigationService(Func<TViewModel> viewModelFactory, MainWindowNavigationStore navigationStore)
	{
		_viewModelFactory = viewModelFactory;
		_navigationStore = navigationStore;
	}

	public void Navigate() =>
		_navigationStore.ViewModel = _viewModelFactory();
}
