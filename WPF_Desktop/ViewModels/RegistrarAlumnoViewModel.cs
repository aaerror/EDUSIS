using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTO;
using Core.ServicioAlumnos.DTO.Request;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels;

public class RegistrarAlumnoViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioAlumno _servicioAlumno;

    private Dictionary<string, List<string>> _errorsByProperty;
    private string _apellido = string.Empty;
    private string _nombre = string.Empty;
    private string _documento = string.Empty;
    private int _sexo;
    private DateTime _fechaNacimento = DateTime.Today.Date;
    private string _nacionalidad = "Argentina";
    private string _email = string.Empty;
    private string _telefono = string.Empty;
    private string _calle = string.Empty;
    private string _altura = string.Empty;
    private int _vivienda;
    private string _barrio = string.Empty;
    private string _observaciones = string.Empty;
    private string _localidad = string.Empty;
    private string _provincia = string.Empty;
    private string _pais = "Argentina";

    public ICommand RegistrarAlumnoCommand { get; }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public RegistrarAlumnoViewModel(IServicioAlumno servicioAlumno)
    {
        _servicioAlumno = servicioAlumno;
        _errorsByProperty = new Dictionary<string, List<string>>();

        RegistrarAlumnoCommand = new ViewModelCommand(ExecuteRegistrarAlumnoCommand, CanExecuteRegistrarAlumnoCommand);
    }

    #region Información Personal
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

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Apellido),
                    new List<string>
                    {
                        "Se debe ingresar el apellido del alumno."
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

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Nombre),
                    new List<string>
                    {
                        "Se debe ingresar el nombre del alumno."
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

            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Documento))
            {
                errors.Add("Se debe ingresar el documento del alumno.");

                _errorsByProperty.Add(nameof(Documento), errors);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Documento)));
            }
            else
            {
                /**
                 * A través de una regular expression verificamos que el dato ingresado sea un número
                 */
                if (Regex.IsMatch(Documento, @"[^0-9]+", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    errors.Add("El documento debe ser un número.");

                    _errorsByProperty.Add(nameof(Documento), errors);
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

            List<string> errors = new List<string>();

            if (FechaNacimiento.Date > DateTime.Now.Date)
            {
                errors.Add("La fecha de nacimento no puede ser mayor a la actual.");
            }

            var edad = DateTime.Now.Year - FechaNacimiento.Date.Year;
            if (edad < 3)
            {
                errors.Add("La edad del alumno es menor a tres (3) años.");
            }

            if (!errors.IsNullOrEmpty())
            {
                _errorsByProperty.Add(nameof(FechaNacimiento), errors);
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
                _errorsByProperty.Add(nameof(Nacionalidad),
                    new List<string>
                    {
                        "Se debe ingresar la nacionalidad del alumno."
                    });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Nacionalidad)));
            }
        }
    }
    #endregion

    #region Contacto
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

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Telefono),
                    new List<string>
                    {
                        "Se debe ingresar un teléfono de contacto."
                    });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Telefono)));
            }
            else
            {
                if (Regex.IsMatch(Telefono, @"[^0-9]+", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Telefono),
                        new List<string>()
                        {
                            "El teléfono debe ser un número."
                        });
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Telefono)));
                }
            }
        }
    }
    #endregion

    #region Dirección
    public string Calle
    {
        get
        {
            return _calle;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Calle));
            _calle = value;
            OnPropertyChanged(nameof(Calle));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Calle),
                    new List<string>()
                    {
                        "Se debe ingresar la calle del domicilio."
                    });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Calle)));
            }
        }
    }

    public string Altura
    {
        get
        {
            return _altura;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Altura));
            _altura = value;
            OnPropertyChanged(nameof(Altura));

            if (!string.IsNullOrWhiteSpace(value))
            {
                if (Regex.IsMatch(Altura, @"[^0-9]+", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Altura),
                        new List<string>()
                        {
                            "La altura debe ser un número."
                        });
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Altura)));
                }
            }
        }
    }

    public int Vivienda
    {
        get
        {
            return _vivienda;
        }

        set
        {
            _vivienda = value;
            OnPropertyChanged(nameof(Vivienda));
        }
    }

    public string Barrio
    {
        get
        {
            return _barrio;
        }

        set
        {
            _barrio = value;
            OnPropertyChanged(nameof(Barrio));
        }
    }

    public string Observaciones
    {
        get
        {
            return _observaciones;
        }

        set
        {
            _observaciones = value;
            OnPropertyChanged(nameof(Observaciones));
        }
    }
    #endregion

    #region Localidad
    public string Localidad
    {
        get
        {
            return _localidad;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Localidad));
            _localidad = value;
            OnPropertyChanged(nameof(Localidad));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Localidad),
                    new List<string>()
                    {
                    "Se debe ingresar la localidad."
                    });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Localidad)));
            }
        }
    }

    public string Provincia
    {
        get
        {
            return _provincia;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Provincia));
            _provincia = value;
            OnPropertyChanged(nameof(Provincia));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Provincia),
                    new List<string>()
                    {
                    "Se debe ingresar la provincia."
                    });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Provincia)));
            }
        }
    }

    public string Pais
    {
        get
        {
            return _pais;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Pais));
            _pais = value;
            OnPropertyChanged(nameof(Pais));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Pais),
                    new List<string>()
                    {
                    "Se debe ingresar el país."
                    });
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Pais)));
            }
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName)
    {
        return _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    }

    public bool HasErrors => _errorsByProperty.Any();
    #endregion

    private bool CanExecuteRegistrarAlumnoCommand(object obj)
    {
        if (HasErrors)
        {
            return false;
        }

        return true;
    }

    private void ExecuteRegistrarAlumnoCommand(object obj)
    {
        var informacionPersonal = new InformacionPersonalRequest
        {
            Apellido = Apellido,
            Nombre = Nombre,
            Documento = Documento,
            Sexo = Sexo,
            FechaNacimiento = FechaNacimiento.Date,
            Nacionalidad = Nacionalidad
        };

        var domicilio = new DomicilioRequest
        {
            Calle = Calle,
            Altura = int.Parse(Altura),
            Vivienda = Vivienda,
            Observacion = Observaciones,
            Localidad = Localidad,
            Provincia = Provincia,
            Pais = Pais
        };

        var contacto = new ContactoRequest
        {
            Email = Email,
            Telefono = Telefono,
        };

        string messageBoxText = $"Datos correctos. ¿Quiere guardar los datos del alumno { Apellido }, { Nombre } ({ Documento })?";
        string caption = "Registrar Alumno";
        var messageBox = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (messageBox == MessageBoxResult.Yes)
        {
            _servicioAlumno.RegistrarAlumno(informacionPersonal, domicilio, contacto);
            MessageBox.Show(messageBoxText, caption);
        }
    }
}
