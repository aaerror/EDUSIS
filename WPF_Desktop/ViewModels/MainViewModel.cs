using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels;

public class MainViewModel : ViewModel
{
    private INavigationService _navigationService;

    public ViewModelCommand GestionAlumnosCommand { get; }


    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;


        GestionAlumnosCommand = new ViewModelCommand(
            command =>
            {
                NavigationService.NavigateTo<GestionAlumnosViewModel>();
            },
            command  => true
        );
    }

    public INavigationService NavigationService
    {
        get
        {
            return _navigationService;
        }

        set
        {
            _navigationService = value;
            OnPropertyChanged(nameof(NavigationService));
        }
    }
}
