using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTO.Response;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels;

public class GestionAlumnosViewModel : ViewModel, INotifyDataErrorInfo
{
    private INavigationService _navigationService;
    private IServicioAlumno _servicioAlumno;
    private Dictionary<string, List<string>> _errorsByProperty;
    
    private string _documentoAlumno = string.Empty;
    private PersonaResponse _personaResponse;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Command
    public ICommand RegistrarAlumnoCommand { get; }

    public ICommand BuscarAlumnoCommand { get; }
    #endregion

    public GestionAlumnosViewModel(INavigationService navigationService, IServicioAlumno servicioAlumno)
    {
        _errorsByProperty = new Dictionary<string, List<string>>();
        _navigationService = navigationService;
        _servicioAlumno = servicioAlumno;

        RegistrarAlumnoCommand = new ViewModelCommand(
            command =>
            {
                navigationService.NavigateTo<RegistrarAlumnoViewModel>();
            });
        BuscarAlumnoCommand = new ViewModelCommand(ExecuteBuscarAlumnoCommand, CanExecuteBuscarAlumnoCommand);
    }

    #region Properties
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
        PersonaResponse = _servicioAlumno.BuscarPorDNI(DocumentoAlumno);
    }
}
