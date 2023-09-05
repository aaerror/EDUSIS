using WPF_Desktop.Shared;

namespace WPF_Desktop.Navigation;


public interface INavigationService
{
    ViewModel VistaActual { get; }

    void NavigateTo<TViewModel>() where TViewModel : ViewModel;
}
