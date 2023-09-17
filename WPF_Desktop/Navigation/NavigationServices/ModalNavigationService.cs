using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.Navigation.NavigationServices;

public class ModalNavigationService<TViewModel> : INavigationService
    where TViewModel : ViewModel
{
    private readonly Func<TViewModel> _viewModelFactory;
    private readonly ModalNavigationStore _modalNavigationStore;


    public ModalNavigationService(Func<TViewModel> viewModelFactory, ModalNavigationStore modalNavigationStore)
    {
        _viewModelFactory = viewModelFactory;
        _modalNavigationStore = modalNavigationStore;
    }

    public void Navigate()
    {
        _modalNavigationStore.ViewModelActual = _viewModelFactory();
    }

}
