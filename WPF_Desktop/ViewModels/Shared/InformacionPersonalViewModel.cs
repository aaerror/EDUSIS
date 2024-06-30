using Core.Shared.DTOs.Personas.Responses;
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
    private string _dni = string.Empty;
    private int _sexo = 0;
    private DateTime _fechaNacimento = DateTime.Now.Date;
    private string _nacionalidad = "Argentina";

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public InformacionPersonalViewModel(InformacionPersonalResponse informacionPersonalResponse)
    {
        if (informacionPersonalResponse is not null)
        {
            Apellido = informacionPersonalResponse.Apellido;
            Nombre = informacionPersonalResponse.Nombre;
            DNI = informacionPersonalResponse.DNI;
            Sexo = informacionPersonalResponse.Sexo;
            FechaNacimiento = informacionPersonalResponse.FechaNacimiento;
            Nacionalidad = informacionPersonalResponse.Nacionalidad;
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
                    "Se debe ingresar el número de documento."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DNI)));
            }
            else
            {
                /**
                 * A través de una regular expression verificamos que el dato ingresado sea un número
                 */
                if (!Regex.IsMatch(DNI, @"^(\d){8}$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(DNI), new List<string>
                    {
                        "Formato de número de documento inválido. Se debe ingresar un número de ocho dígitos."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DNI)));
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
