using System;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

public class GestionLicenciasNavigationService<TViewModel> : INavigationService
    where TViewModel : ViewModel
{
    private Func<TViewModel> _viewModelFactory;
    private NavigationStore _navigationStore;


    public GestionLicenciasNavigationService(Func<TViewModel> viewModelFactory, NavigationStore navigationStore)
    {
        _viewModelFactory = viewModelFactory;
        _navigationStore = navigationStore;
    }

    public void Navigate()
    {
        _navigationStore.ViewModelActual = _viewModelFactory();
    }
}