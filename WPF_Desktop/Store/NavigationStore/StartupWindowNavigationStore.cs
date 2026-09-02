using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace WPF_Desktop.Store.NavigationStore;

internal class StartupWindowNavigationStore
{
	private ObservableObject _viewModel;

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

	public void OnViewModelChanged() =>
		ViewModelChanged?.Invoke();
}