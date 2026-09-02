using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Store.NavigationStore;

namespace WPF_Desktop.Navigation.NavigationServices.Modal;

internal class ModalNavigationService<TViewModel> : INavigationService
	where TViewModel : ObservableObject
{
	private readonly Func<TViewModel> _viewModelFactory;
	private readonly ModalWindowNavigationStore _modalNavigationStore;


	public ModalNavigationService(Func<TViewModel> viewModelFactory, ModalWindowNavigationStore modalNavigationStore)
	{
		_viewModelFactory = viewModelFactory;
		_modalNavigationStore = modalNavigationStore;
	}

	public void Navigate()
	{
		_modalNavigationStore.ViewModel = _viewModelFactory();
	}
}