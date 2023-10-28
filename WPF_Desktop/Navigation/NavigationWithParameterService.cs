using System;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.Navigation;

public class NavigationWithParameterService<TParameter, TViewModel>
    where TViewModel : ViewModel
{
    private Func<TParameter, TViewModel> _viewModelFactory;
    private readonly NavigationStore _navigationStore;


    public NavigationWithParameterService(Func<TParameter, TViewModel> viewModelFactory, NavigationStore navigationStore)
    {
        _viewModelFactory = viewModelFactory;
        _navigationStore = navigationStore;
    }

    public void Navigation(TParameter parameter)
    {
        _navigationStore.ViewModelActual = _viewModelFactory(parameter);
    }
}