using Core.Shared.DTOs.Personas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Shared;

public class InformacionPersonalViewModel : ViewModel, INotifyDataErrorInfo
{
    private string _apellido = string.Empty;
    private string _nombre = string.Empty;
    private string _documento = string.Empty;
    private int _sexo = 0;
    private DateTime _fechaNacimento = DateTime.Now;
    private string _nacionalidad = "Argentina";

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public InformacionPersonalViewModel(InformacionPersonalDTO informacionPersonalDTO)
    {
        if (informacionPersonalDTO is not null)
        {
            Apellido = informacionPersonalDTO.Apellido;
            Nombre = informacionPersonalDTO.Nombre;
            Documento = informacionPersonalDTO.Documento;
            Sexo = informacionPersonalDTO.Sexo;
            FechaNacimiento = informacionPersonalDTO.FechaNacimiento;
            Nacionalidad = informacionPersonalDTO.Nacionalidad;
        }
    }

    #region Properties
    public string Apellido
    {
        get
        {
            return _apellido;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Apellido));
            _apellido = value;
            OnPropertyChanged(nameof(Apellido));

            if (string.IsNullOrWhiteSpace(Apellido))
            {
                _errorsByProperty.Add(nameof(Apellido), new List<string>
                {
                    "Se debe ingresar el apellido."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Apellido)));
            }
        }
    }

    public string Nombre
    {
        get
        {
            return _nombre;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Nombre));
            _nombre = value;
            OnPropertyChanged(nameof(Nombre));

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                _errorsByProperty.Add(nameof(Nombre), new List<string>
                {
                    "Se debe ingresar el nombre."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Nombre)));
            }
        }
    }

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
                    "Se debe ingresar el número de documento."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Documento)));
            }
            else
            {
                /**
                 * A través de una regular expression verificamos que el dato ingresado sea un número
                 */
                if (!Regex.IsMatch(Documento, @"^(\d){8}$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Documento), new List<string>
                    {
                        "Formato de número de documento inválido. Se debe ingresar un número de ocho dígitos."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Documento)));
                }

                /*var esValido = _servicioAlumno.EsDocumentoValido(Documento);
                if (!esValido)
                {
                    errors.Add("El número de documento ya se encuentra registrado en el sistema.");
                }*/
            }
        }
    }

    public int Sexo
    {
        get
        {
            return _sexo;
        }

        set
        {
            _sexo = value;
            OnPropertyChanged(nameof(Sexo));
        }
    }

    public DateTime FechaNacimiento
    {
        get
        {
            return _fechaNacimento;
        }

        set
        {
            _errorsByProperty.Remove(nameof(FechaNacimiento));
            _fechaNacimento = value;
            OnPropertyChanged(nameof(FechaNacimiento));

            if (FechaNacimiento.Date > DateTime.Now.Date)
            {
                _errorsByProperty.Add(nameof(FechaNacimiento), new List<string>
                {
                    "La fecha de nacimento no puede ser mayor a la actual."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FechaNacimiento)));
            }
        }
    }

    public string Nacionalidad
    {
        get
        {
            return _nacionalidad;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Nacionalidad));
            _nacionalidad = value;
            OnPropertyChanged(nameof(Nacionalidad));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Nacionalidad), new List<string>
                {
                    "Se debe ingresar la nacionalidad del alumno."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Nacionalidad)));
            }
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion
}
