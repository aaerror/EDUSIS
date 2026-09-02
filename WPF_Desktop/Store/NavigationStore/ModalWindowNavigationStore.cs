using CommunityToolkit.Mvvm.ComponentModel;
using System;
using WPF_Desktop.Shared;

namespace WPF_Desktop.Store.NavigationStore;

internal class ModalWindowNavigationStore
{
	private ObservableObject _viewModel;

	public bool EstaAbierto => _viewModel != null;

	public event Action ViewModelChanged;


	public ObservableObject ViewModel
	{
		get
		{
			return _viewModel;
		}

		set
		{
			//_viewModel?.Dispose();
			_viewModel = value;
			OnViewModelChanged();
		}
	}

	public void Cerrar()
	{
		ViewModel = null;
	}

	public void OnViewModelChanged() =>
		ViewModelChanged?.Invoke();
}