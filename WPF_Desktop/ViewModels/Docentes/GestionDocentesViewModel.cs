using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Docentes;

public class GestionDocentesViewModel : ViewModel, INotifyDataErrorInfo
{
    #region Servicios
    private readonly IServicioDocente _servicioDocentes;
    #endregion

    #region NavigationService
    private readonly INavigationService _registrarDocenteNavigationService;
    private readonly INavigationService _perfilDocenteNavigationService;
    private readonly INavigationService _gestionPuestosNavigationService;
    private readonly INavigationService _gestionLicenciasNavigationService;
    #endregion

    private LegajoStore _perfilBuscadoStore;

    private LegajoDocenteViewModel _legajoDocenteViewModel;
    private ObservableCollection<LegajoDocenteViewModel> _legajosDocentes = new ObservableCollection<LegajoDocenteViewModel>();

    private string _nombreCompleto = string.Empty;
    private string _dni = string.Empty;
    private bool _mostrarVista;

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();

    public bool HasErrors => _errorsByProperty.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand RegistrarDocenteCommand { get; }
    public ViewModelCommand BuscarCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    public ViewModelCommand NavigationCommand { get; }
    #endregion


    public GestionDocentesViewModel(IServicioDocente servicioDocentes,
                                    INavigationService registrarDocenteNavigationService,
                                    INavigationService perfilDocenteNavigationService,
                                    INavigationService gestionPuestoNavigationService,
                                    INavigationService gestionLicenciasNavigationService,
                                    LegajoStore perfilBuscadoStore)
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
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
        NavigationCommand = new ViewModelCommand(ExecuteNavigationCommand, CanExecuteNavigationCommand);

        MostrarVista = false;
    }

    private void LoadLegajosDocente(IEnumerable<LegajoDocenteViewModel> legajosDocentes)
    {
        LegajosDocentes.Clear();
        foreach (var legajoDocente in legajosDocentes)
        {
            LegajosDocentes.Add(legajoDocente);
        }
    }

    #region Properties
    public string NombreCompleto
    {
        get
        {
            return _nombreCompleto;
        }

        set
        {
            _errorsByProperty.Remove(nameof(NombreCompleto));
            _nombreCompleto = value;
            OnPropertyChanged(nameof(NombreCompleto));
        }
    }

    public string DNI
    {
        get
        {
            return _dni;
        }

        set
        {
            _errorsByProperty.Remove(nameof(DNI));
            _dni = value;
            OnPropertyChanged(nameof(DNI));

            if (string.IsNullOrWhiteSpace(DNI))
            {
                _errorsByProperty.Add(nameof(DNI), new List<string>
                {
                    "Se debe ingresar el DNI del docente que desea buscar."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DNI)));
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

    public LegajoDocenteViewModel LegajoDocenteViewModel
    {
        get
        {
            return _legajoDocenteViewModel;
        }

        set
        {
            _legajoDocenteViewModel = value;
            OnPropertyChanged(nameof(LegajoDocenteViewModel));
        }
    }

    public ObservableCollection<LegajoDocenteViewModel> LegajosDocentes
    {
        get
        {
            return _legajosDocentes;
        }

        set
        {
            _legajosDocentes = value;
            OnPropertyChanged(nameof(LegajosDocentes));
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

        switch (obj)
        {
/*
            case "DNI":
                if (string.IsNullOrWhiteSpace(DNI))
                {
                    MessageBox.Show("Se deben ingresar el DNI del docente que desea consultar.", "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                    DNI = string.Empty;

                    break;
                }

                try
                {
                    var legajoDocente = _servicioDocentes.BuscarLegajoDocentePorDNI(DNI);
                    LegajoDocenteViewModel = new LegajoDocenteViewModel(legajoDocente);
                    MostrarVista = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                break;
*/
            case "NombreCompleto":
                try
                {
                    var response = _servicioDocentes.BuscarLegajoDocentePorApellidoNombre(
                        new BuscarDocentePorApellidoNombreRequest(NombreCompleto));

                    if (response.Count is 0)
                    {
                        messageBoxText = $"No se ha encontrado ningún docente. Vuelva a intentarlo nuevamente.";
                        caption = "Resultado de la búsqueda";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                        break;
                    }

                    MostrarVista = true;
                    LoadLegajosDocente(response.Select(x =>
                        new LegajoDocenteViewModel(x)));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                break;
        }
    }
    #endregion

    #region EliminarCommand
    private bool CanExecuteEliminarCommand(object obj)
    {
        bool canExecute = false;
        if (LegajoDocenteViewModel is not null)
        {
            canExecute = true;
        }

        return canExecute;
    }

    private void ExecuteEliminarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (LegajoDocenteViewModel is null)
        {
            messageBoxText = "Se debe buscar previamente el docente para poder realizar los cambios que necesite.";
            caption = "Quitar Docente";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        messageBoxText = $"¿Está seguro que desea quitar el docente { LegajoDocenteViewModel.NombreCompleto }?";
        caption = "Quitar Docente";
        result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _servicioDocentes.QuitarDocente(LegajoDocenteViewModel.DocenteID);
                messageBoxText = $"El docente, { LegajoDocenteViewModel.NombreCompleto }, se quitó correctamente.";
                caption = "Operación Exitosa";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                LegajoDocenteViewModel = new LegajoDocenteViewModel(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region NavigationCommand
    private bool CanExecuteNavigationCommand(object obj)
    {
        bool canExecute = false;
        if (LegajoDocenteViewModel is not null)
        {
            canExecute = true;
        }

        return canExecute;
    }

    private void ExecuteNavigationCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;

        if (LegajoDocenteViewModel is null)
        {
            messageBoxText = "Se debe buscar previamente el docente que desea visitar el perfil.";
            caption = "Perfil Docente";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        _perfilBuscadoStore.PersonaID = LegajoDocenteViewModel.DocenteID;
        _perfilBuscadoStore.Documento = LegajoDocenteViewModel.CUIL;

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
