using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using WPF_Desktop.Navigation;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Alumnos;

public class GestionAlumnosViewModel : ViewModel, INotifyDataErrorInfo
{
    private IServicioAlumnos _servicioAlumnos;
    private INavigationService _registrarAlumnoNavigationService;
    private INavigationService _verPerfilNavigationService;
    private PerfilBuscadoStore _perfilBuscadoStore;

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Any();

    private string _documentoAlumno = string.Empty;
    private PersonaResponse _personaResponse;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand RegistrarAlumnoCommand { get; }

    public ViewModelCommand BuscarAlumnoCommand { get; }

    public ViewModelCommand QuitarCommand { get;  }

    public ViewModelCommand VerPerfilCommand { get; }
    #endregion


    public GestionAlumnosViewModel(IServicioAlumnos servicioAlumnos,
                                   INavigationService registrarAlumnoNavigationService,
                                   INavigationService verPerfilNavigationService,
                                   PerfilBuscadoStore perfilBuscadoStore)
    {
        _servicioAlumnos = servicioAlumnos;
        _registrarAlumnoNavigationService = registrarAlumnoNavigationService;
        _verPerfilNavigationService = verPerfilNavigationService;
        _perfilBuscadoStore = perfilBuscadoStore;

        BuscarAlumnoCommand = new ViewModelCommand(ExecuteBuscarAlumnoCommand, CanExecuteBuscarAlumnoCommand);
        RegistrarAlumnoCommand = new ViewModelCommand(command =>
        {
            _registrarAlumnoNavigationService.Navigate();
        });
        QuitarCommand = new ViewModelCommand(ExecuteQuitarCommand, CanExecuteQuitarCommand);
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
                if (!Regex.IsMatch(DocumentoAlumno, @"^\d{8}$", RegexOptions.None, TimeSpan.FromMilliseconds(2000)))
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
            PersonaResponse = _servicioAlumnos.BuscarPorDNI(DocumentoAlumno);
            _perfilBuscadoStore.Documento = PersonaResponse.Documento;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error al buscar el alumno", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
    #endregion


    #region QuitarCommand
    private bool CanExecuteQuitarCommand(object obj)
    {
        bool canExecute = false;
        if (!string.IsNullOrWhiteSpace(_perfilBuscadoStore.Documento))
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

        if (PersonaResponse is null)
        {
            messageBoxText = "Se debe buscar previamente el alumno para poder realizar los cambios que necesite.";
            caption = "Quitar Alumno";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            messageBoxText = $"¿Está seguro que desea quitar el alumno {PersonaResponse.Apellido}, {PersonaResponse.Nombre}?";
            caption = "Quitar Alumno";
            result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _servicioAlumnos.QuitarAlumno(PersonaResponse.PersonaId);
                    messageBoxText = $"El alumno, {PersonaResponse.Apellido}, {PersonaResponse.Nombre}, se quitó correctamente.";
                    caption = "Operación Exitosa";
                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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