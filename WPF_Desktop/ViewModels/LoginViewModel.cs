using Core.ServicioAccesos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels;

public class LoginViewModel : ViewModel, INotifyDataErrorInfo
{
    private IServicioAcceso _servicioAcceso;
    private Dictionary<string, List<string>> _errorsByProperty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string  _year = DateTime.Now.Year.ToString();
    private string _mensajeError;

    public ICommand LoginCommand { get; }
    public ICommand RecuperarPasswordCommand { get; }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public LoginViewModel(IServicioAcceso acceso)
    {
        _servicioAcceso = acceso;
        _errorsByProperty = new Dictionary<string, List<string>>();
        LoginCommand = new ViewModelCommand(ExecuteLoginCommand, CanExecuteLoginCommand);
    }

    public string Email
    {
        get
        {
            return _email;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Email));
            _email = value;
            OnPropertyChanged(nameof(Email));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Email),
                    new List<string>
                    {
                        "Se debe ingresar un correo electrónico."
                    });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
            }
            else
            {
                if (!Regex.IsMatch(Email, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Email),
                        new List<string>
                        {
                            "Correo electrónico inválido."
                        });
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
                }
            }
        }
    }

    public string Password
    {
        get { return _password; }
        set
        {
            _password = value;
            OnPropertyChanged(nameof(Password));
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

    private bool CanExecuteLoginCommand(object obj)
    {
        return false;
    }

    private void ExecuteLoginCommand(object obj)
    {
        throw new NotImplementedException();
    }
}