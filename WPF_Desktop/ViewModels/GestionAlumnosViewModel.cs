using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WPF_Desktop.Navigation;
using WPF_Desktop.Navigation.NavigationServices;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels;

public class GestionAlumnosViewModel : ViewModel, INotifyDataErrorInfo
{
    private IServicioAlumnos _servicioAlumno;
    private INavigationService _registrarAlumnoNavigationService;
    private INavigationService _verPerfilNavigationService;
    private Dictionary<string, List<string>> _errorsByProperty;

    private PerfilBuscadoStore _perfilBuscadoStore;
    private string _documentoAlumno = string.Empty;
    private PersonaResponse _personaResponse;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ICommand RegistrarAlumnoCommand { get; }

    public ICommand BuscarAlumnoCommand { get; }

    public ICommand VerPerfilCommand { get; }
    #endregion

    public GestionAlumnosViewModel(INavigationService registrarNavigationService,
                                   INavigationService verPerfilNavigationService,
                                   PerfilBuscadoStore perfilBuscadoStore,
                                   IServicioAlumnos servicioAlumno)
    {
        _errorsByProperty = new Dictionary<string, List<string>>();
        _servicioAlumno = servicioAlumno;
        _registrarAlumnoNavigationService = registrarNavigationService;
        _verPerfilNavigationService = verPerfilNavigationService;

        _perfilBuscadoStore = perfilBuscadoStore;

        BuscarAlumnoCommand = new ViewModelCommand(ExecuteBuscarAlumnoCommand, CanExecuteBuscarAlumnoCommand);
        RegistrarAlumnoCommand = new ViewModelCommand(command =>
            {
                _registrarAlumnoNavigationService.Navigate();
            });

        VerPerfilCommand = new ViewModelCommand(ExecuteVerPerfilCommand, CanExecuteVerPerfilCommand);
    }

    #region Properties
    public string DocumentoAlumno
    {
        get
        {
            return _documentoAlumno;
        }

        set
        {
            _errorsByProperty.Remove(nameof(DocumentoAlumno));
            _documentoAlumno = value;
            OnPropertyChanged(nameof(DocumentoAlumno));

            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(DocumentoAlumno))
            {
                errors.Add("Se debe ingresar el documento del alumno.");

                _errorsByProperty.Add(nameof(DocumentoAlumno), errors);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DocumentoAlumno)));
            }
            else
            {
                /**
                 * A través de una regular expression verificamos que el dato ingresado sea un número
                 */
                if (Regex.IsMatch(DocumentoAlumno, @"[^0-9]+", RegexOptions.None, TimeSpan.FromMilliseconds(2000)))
                {
                    errors.Add("El documento debe ser un número.");

                    _errorsByProperty.Add(nameof(DocumentoAlumno), errors);
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DocumentoAlumno)));
                }
            }
        }
    }

    public PersonaResponse PersonaResponse
    {
        get
        {
            return _personaResponse;
        }

        set
        {
            _personaResponse = value;
            OnPropertyChanged(nameof(PersonaResponse));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName)
    {
        return _errorsByProperty.GetValueOrDefault(propertyName, new List<string>());
    }

    public bool HasErrors => _errorsByProperty.Any();
    #endregion

    #region BuscarAlumnoCommand
    private bool CanExecuteBuscarAlumnoCommand(object obj)
    {
        bool canExecute = false;
        if (!string.IsNullOrWhiteSpace(DocumentoAlumno))
        {
            canExecute = true;
        }

        return canExecute;
    }

    private void ExecuteBuscarAlumnoCommand(object obj)
    {
        try
        {
            PersonaResponse = _servicioAlumno.BuscarPorDNI(DocumentoAlumno);
            _perfilBuscadoStore.Documento = PersonaResponse.Documento;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error al buscar el alumno", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
    }
    #endregion

    #region VerPerfilAlumnoCommand
    private bool CanExecuteVerPerfilCommand(object obj)
    {
        bool canExecute = false;
        if (!string.IsNullOrWhiteSpace(_perfilBuscadoStore.Documento))
        {
            canExecute = true;
        }

        return canExecute;
    }

    private void ExecuteVerPerfilCommand(object obj)
    {
        try
        {
            _verPerfilNavigationService.Navigate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error al ver el perfil", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion
}