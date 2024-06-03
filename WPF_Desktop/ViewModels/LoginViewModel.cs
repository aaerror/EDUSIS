using Core.ServicioAutenticaciones;
using Core.ServicioAutenticaciones.DTOs.Request;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels;

public class LoginViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioAutenticacion _servicioAutenticacion;
    private LoginRequest _loginRequest;

    private Dictionary<string, List<string>> _errorsByProperty;
    private string _usuario = string.Empty;
    private String _clave;
    private string  _year = DateTime.Now.Year.ToString();
    private string _mensajeError;

    public ICommand LoginCommand { get; }
    public ICommand RecuperarPasswordCommand { get; }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public LoginViewModel(IServicioAutenticacion servicioAutenticacion)
    {
        _servicioAutenticacion = servicioAutenticacion;
        _errorsByProperty = new Dictionary<string, List<string>>();
        LoginCommand = new ViewModelCommand(ExecuteLoginCommand, CanExecuteLoginCommand);
    }

    public string Usuario
    {
        get
        {
            return _usuario;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Usuario));
            _usuario = value;
            OnPropertyChanged(nameof(Usuario));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Usuario),
                    new List<string>
                    {
                        "Se debe ingresar un correo electrónico."
                    });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Usuario)));
            }
            else
            {
                if (!Regex.IsMatch(Usuario, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Usuario),
                        new List<string>
                        {
                            "Correo electrónico inválido."
                        });
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Usuario)));
                }
            }
        }
    }

    public String Clave
    {
        get { return _clave; }
        set
        {
            _clave = value;
            OnPropertyChanged(nameof(Clave));
        }
    }

    public string Year
    {
        get { return _year; }
    }

    public string MensajeError
    {
        get { return _mensajeError; }
        set
        {
            _mensajeError = value;
            OnPropertyChanged(nameof(MensajeError));
        }
    }

    #region DataError

    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName, new List<string>());

    public bool HasErrors => _errorsByProperty.Any();
    #endregion

    private bool CanExecuteLoginCommand(object obj) => !HasErrors;

    private void ExecuteLoginCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Clave))
        {
            messageBoxText = "Se deben completar todos los campos necesarios para poder ingresar.";
            caption = "Error";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            Usuario = string.Empty;
            Clave = string.Empty;

            return;
        }

        try
        {
            _loginRequest = new LoginRequest(Usuario, Clave);
            _servicioAutenticacion.Login(_loginRequest);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error al Ingresar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}