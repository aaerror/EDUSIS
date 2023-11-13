using Core.ServicioDocentes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Docentes;

public class GestionDocentesViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioDocentes _servicioDocentes;
    private readonly INavigationService _registrarDocenteNavigationService;
    private readonly INavigationService _perfilDocenteNavigationService;
    private readonly INavigationService _gestionPuestosNavigationService;
    private readonly INavigationService _gestionLicenciasNavigationService;

    private PerfilBuscadoStore _perfilBuscadoStore;

    private DocenteInfoViewModel _docenteInfoViewModel;

    private string _documento = string.Empty;
    private bool _mostrarVista;

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand RegistrarDocenteCommand { get; }
    public ViewModelCommand BuscarCommand { get; }
    public ViewModelCommand QuitarCommand { get; }
    public ViewModelCommand NavigationCommand { get; }
    #endregion


    public GestionDocentesViewModel(IServicioDocentes servicioDocentes,
                                    INavigationService registrarDocenteNavigationService,
                                    INavigationService perfilDocenteNavigationService,
                                    INavigationService gestionPuestoNavigationService,
                                    INavigationService gestionLicenciasNavigationService,
                                    PerfilBuscadoStore perfilBuscadoStore)
    {
        _servicioDocentes = servicioDocentes;
        _registrarDocenteNavigationService = registrarDocenteNavigationService;
        _perfilDocenteNavigationService = perfilDocenteNavigationService;
        _gestionPuestosNavigationService = gestionPuestoNavigationService;
        _gestionLicenciasNavigationService = gestionLicenciasNavigationService;
        _perfilBuscadoStore = perfilBuscadoStore;

        RegistrarDocenteCommand = new ViewModelCommand(commnad =>
        {
            _registrarDocenteNavigationService.Navigate();
        });
        BuscarCommand = new ViewModelCommand(ExecuteBuscarCommand, CanExecuteBuscarCommand);
        QuitarCommand = new ViewModelCommand(ExecuteQuitarCommand, CanExecuteQuitarCommand);
        NavigationCommand = new ViewModelCommand(ExecuteNavigationCommand, CanExecuteNavigationCommand);

        MostrarVista = false;
    }

    #region Properties
    public string Documento
    {
        get
        {
            return _documento;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Documento));
            _documento = value;
            OnPropertyChanged(nameof(Documento));

            if (string.IsNullOrWhiteSpace(Documento))
            {
                _errorsByProperty.Add(nameof(Documento), new List<string>
                {
                    "Se debe ingresar el DNI del docente que desea buscar."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Documento)));
            }
        }
    }

    public bool MostrarVista
    {
        get
        {
            return _mostrarVista;
        }

        set
        {
            _mostrarVista = value;
            OnPropertyChanged(nameof(MostrarVista));
        }
    }

    public DocenteInfoViewModel DocenteInfoViewModel
    {
        get
        {
            return _docenteInfoViewModel;
        }

        set
        {
            _docenteInfoViewModel = value;
            OnPropertyChanged(nameof(DocenteInfoViewModel));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion

    #region BuscarCommand
    private bool CanExecuteBuscarCommand(object obj) => !HasErrors;

    private void ExecuteBuscarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (string.IsNullOrWhiteSpace(Documento))
        {
            messageBoxText = "Se deben ingresar el DNI del docente que desea consultar.";
            caption = "Error al Consultar Docente";

            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            Documento = string.Empty;

            return;
        }

        try
        {
            var docente = _servicioDocentes.BuscarDocentePorDNI(Documento);
            DocenteInfoViewModel = new DocenteInfoViewModel(docente);
            MostrarVista = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    #endregion

    #region QuitarCommand
    private bool CanExecuteQuitarCommand(object obj)
    {
        bool canExecute = false;
        if (DocenteInfoViewModel is not null)
        {
            canExecute = true;
        }

        return canExecute;
    }

    private void ExecuteQuitarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (DocenteInfoViewModel is null)
        {
            messageBoxText = "Se debe buscar previamente el docente para poder realizar los cambios que necesite.";
            caption = "Quitar Docente";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        messageBoxText = $"¿Está seguro que desea quitar el docente { DocenteInfoViewModel.NombreCompleto }?";
        caption = "Quitar Docente";
        result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _servicioDocentes.QuitarDocente(DocenteInfoViewModel.DocenteID);
                messageBoxText = $"El docente, { DocenteInfoViewModel.NombreCompleto }, se quitó correctamente.";
                caption = "Operación Exitosa";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                DocenteInfoViewModel = new DocenteInfoViewModel(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region NavigationCommand
    private bool CanExecuteNavigationCommand(object obj)
    {
        bool canExecute = false;
        if (DocenteInfoViewModel is not null)
        {
            canExecute = true;
        }

        return canExecute;
    }

    private void ExecuteNavigationCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;

        if (DocenteInfoViewModel is null)
        {
            messageBoxText = "Se debe buscar previamente el docente que desea visitar el perfil.";
            caption = "Perfil Docente";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        _perfilBuscadoStore.PersonaID = DocenteInfoViewModel.DocenteID;
        _perfilBuscadoStore.Documento = DocenteInfoViewModel.Institucional.Documento;

        switch (obj)
        {
            case "Licencia":
                _gestionLicenciasNavigationService.Navigate();
                break;
            case "Perfil":
                _perfilDocenteNavigationService.Navigate();
                break;
            case "Puesto":
                _gestionPuestosNavigationService.Navigate();
                break;
        }
    }
    #endregion
}
