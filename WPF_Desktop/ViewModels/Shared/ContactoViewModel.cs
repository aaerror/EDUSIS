using Core.Shared.DTOs.Personas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Shared;

public class ContactoViewModel : ViewModel, INotifyDataErrorInfo
{
    private string _telefono = string.Empty;
    private string _email = string.Empty;

    private Dictionary<string, List<string>> _errorsByProperty = new();

    public bool HasErrors => _errorsByProperty.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public ContactoViewModel(ContactoDTO contactoDTO)
    {
        if (contactoDTO is not null)
        {
            Telefono = contactoDTO.Telefono;
            Email = contactoDTO.Email;
        }
    }

    #region Properties
    public string Telefono
    {
        get
        {
            return _telefono;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Telefono));
            _telefono = value;
            OnPropertyChanged(nameof(Telefono));

            if (string.IsNullOrWhiteSpace(Telefono))
            {
                _errorsByProperty.Add(nameof(Telefono), new List<string>
                {
                    "Se debe especificar un número de telefono."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Telefono)));
            }
            else
            {
                if (Regex.IsMatch(Telefono, @"[^0-9]+", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Telefono), new List<string>()
                    {
                        "El número de teléfono debe ser un número."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Telefono)));
                }
            }
        }
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

            if (string.IsNullOrWhiteSpace(Email))
            {
                _errorsByProperty.Add(nameof(Email), new List<string>
                {
                    "Se debe ingresar un correo electrónico."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
            }
            else
            {
                if (!Regex.IsMatch(Email, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Email), new List<string>
                    {
                        "Correo electrónico inválido."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Email)));
                }
            }
        }
    }
    #endregion

    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
}