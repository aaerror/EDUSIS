using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels;

public class MainViewModel : ViewModel
{
    private ModalNavigationStore _modalNavigationStore;
    private NavigationStore _navigationStore;

    public ViewModel ViewModelActual => _navigationStore.ViewModelActual;
    public ViewModel ModalViewModelActual => _modalNavigationStore.ViewModelActual;
    public bool ModalEstaAbierto => _modalNavigationStore.EstaAbierto;

    #region Commands
    public ViewModelCommand GestionAlumnosCommand { get; }
    #endregion

    public MainViewModel(ModalNavigationStore modalNavigationStore, NavigationStore navigationStore, INavigationService gestionAlumnoNavigationService)
    {
        _modalNavigationStore = modalNavigationStore;
        _navigationStore = navigationStore;

        GestionAlumnosCommand = new ViewModelCommand(command =>
            {
                gestionAlumnoNavigationService.Navigate();
            });

        _navigationStore.ViewModelActualChanged += OnViewModelChanged;
        _modalNavigationStore.ViewModelActualChanged += OnModalViewModelChanged;
    }

    private void OnViewModelChanged()
    {
        OnPropertyChanged(nameof(ViewModelActual));
    }

    private void OnModalViewModelChanged()
    {
        OnPropertyChanged(nameof(ModalViewModelActual));
        OnPropertyChanged(nameof(ModalEstaAbierto));
    }
}
