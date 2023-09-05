using System;
using System.ComponentModel;
using WPF_Desktop.Shared;

namespace WPF_Desktop.Navigation;

public class NavigationService : INotifyPropertyChanged, INavigationService 
{
    private readonly Func<Type, ViewModel> _viewModelFactory;
    private ViewModel _vistalActual;

    public event PropertyChangedEventHandler? PropertyChanged;


    public NavigationService(Func<Type, ViewModel> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    public ViewModel VistaActual
    {
        get
        {
            return _vistalActual;
        }

        private set
        {
            _vistalActual = value;
            OnPropertyChanged(nameof(VistaActual));
        }
    }

    public void OnPropertyChanged(string parameter)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(parameter));
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModel
    {
        ViewModel newViewModel = _viewModelFactory.Invoke(typeof(TViewModel));
        VistaActual = newViewModel;
    }
}
