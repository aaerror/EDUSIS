using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.Navigation.NavigationServices;

public class VerPerfilNavigationService<TViewModel> : INavigationService
    where TViewModel : ViewModel
{
    private readonly Func<TViewModel> _viewModelFactory;
    private readonly NavigationStore _navigationStore;


    public VerPerfilNavigationService(Func<TViewModel> viewModelFactory, NavigationStore navigationStore)
    {
        _viewModelFactory = viewModelFactory;
        _navigationStore = navigationStore;
    }

    public void Navigate()
    {
        _navigationStore.ViewModelActual = _viewModelFactory();
    }
}
