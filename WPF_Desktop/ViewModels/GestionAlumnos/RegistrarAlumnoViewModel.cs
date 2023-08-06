using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTO;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace WPF_Desktop.ViewModels.GestionAlumnos;

public class RegistrarAlumnoViewModel : ViewModel, IDataErrorInfo
{
    private IServicioAlumno _servicioAlumno;
    private string _apellido = "Ingresar apellido";
    private string _nombre = "Ingresar nombre";
    private string _documento = "00000000";
    private int _sexo;
    private DateTime _fechaNacimento = DateTime.Today.Date;
    private string _nacionalidad = "Argentina";
    private string _contactoDescripcion = String.Empty;
    private int _tipoContacto;
    private string _calle = "Ingresar calle";
    private string _altura = "0000";
    private int _vivienda;
    private string _barrio = String.Empty;
    private string _observaciones = String.Empty;
    private string _localidad = "Ingresar localidad";
    private string _provincia = "Ingresar provincia";
    private string _pais = "Argentina";


    public ICommand RegistrarAlumno { get; }

    public RegistrarAlumnoViewModel()
    {
        _servicioAlumno = new ServicioAlumno();
        RegistrarAlumno = new ViewModelComand(ExecuteRegistrarAlumnoCommand, CanExecuteRegistrarAlumno);
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
            _apellido = value;
            OnPropertyChanged(nameof(Apellido));
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
            _nombre = value;
            OnPropertyChanged(nameof(Nombre));
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
            _documento = value;
            OnPropertyChanged(nameof(Documento));
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
            _fechaNacimento = value;
            OnPropertyChanged(nameof(FechaNacimiento));
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
            _nacionalidad = value;
            OnPropertyChanged(nameof(Nacionalidad));
        }
    }
    #endregion

    #region Contacto
    public int TipoContacto
    {
        get
        {
            return _tipoContacto;
        }

        set
        {
            _tipoContacto = value;
            OnPropertyChanged(nameof(TipoContacto));
        }
    }

    public string ContactoDescripcion
    {
        get
        {
            return _contactoDescripcion;
        }

        set
        {
            _contactoDescripcion = value;
            OnPropertyChanged(nameof(ContactoDescripcion));
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
            _calle = value;
            OnPropertyChanged(nameof(Calle));
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
            _altura = value;
            OnPropertyChanged(nameof(Altura));
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
            _localidad = value;
            OnPropertyChanged(nameof(Localidad));
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
            _provincia = value;
            OnPropertyChanged(nameof(Provincia));
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
            _pais = value;
            OnPropertyChanged(nameof(Pais));
        }
    }
    #endregion

    #region DataError
    public string this[string columnName]
    {
        get
        {
            string result = String.Empty;
            if (columnName == "Apellido")
            {
                if (string.IsNullOrWhiteSpace(Apellido))
                {
                    result = "Se debe ingresar un apellido para el alumno.";
                }
            }

            if (columnName == "Nombre")
            {
                if (string.IsNullOrWhiteSpace(Nombre))
                {
                    result = "Se debe ingresar un nombre para el alumno.";
                }
            }

            if (columnName == "Documento")
            {
                if (string.IsNullOrWhiteSpace(Documento))
                {
                    result = "El documento no puede estar vacío.";
                }

                int numeroDocumento;
                var dni = Int32.TryParse(Documento, out numeroDocumento);
                if (!dni)
                {
                    result = "El documento debe ser un número.";
                }

                var esValido = _servicioAlumno.EsDocumentoValido(Documento);
                if (!esValido)
                {
                    result = "El número de documento ya se encuentra registrado en el sistema.";
                }
            }

            /*if (columnName == "FechaNacimiento")
            {
                if (DateTime.Now.Date > FechaNacimiento.Date)
                {
                    result = "La fecha de nacimento no puede ser mayor a la actual.";
                }
            }*/

            if (columnName == "Nacionalidad")
            {
                if (string.IsNullOrWhiteSpace(Nacionalidad))
                {
                    result = "Se debe ingresar la nacionalidad del alumno.";
                }
            }

            if (columnName == "Calle")
            {
                if (string.IsNullOrWhiteSpace(Calle))
                {
                    result = "Se debe el nombre de la calle.";
                }
            }

            if (columnName == "Localidad")
            {
                if (string.IsNullOrWhiteSpace(Localidad))
                {
                    result = "Se debe ingresar una localidad.";
                }
            }

            if (columnName == "Provincia")
            {
                if (string.IsNullOrWhiteSpace(Provincia))
                {
                    result = "Se debe ingresar una provincia.";
                }
            }

            if (columnName == "Pais")
            {
                if (string.IsNullOrWhiteSpace(Localidad))
                {
                    result = "Se debe ingresar una país.";
                }
            }

            return result;
        }
    }

    public string Error
    {
        get
        {
            return string.Empty;
        }
    }
    #endregion

    // TODO: Hacer validaciones de los datos ingresados
    private bool CanExecuteRegistrarAlumno(object obj)
    {
        if (!string.IsNullOrWhiteSpace(Error))
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
            Altura = Int32.Parse(Altura),
            Vivienda = Vivienda,
            Observacion = Observaciones,
            Localidad = Localidad,
            Provincia = Provincia,
            Pais = Pais
        };

        ContactoRequest? contacto = null;
        if (!string.IsNullOrWhiteSpace(ContactoDescripcion))
        {
            contacto = new ContactoRequest
            {
                TipoContacto = TipoContacto,
                Descripcion = ContactoDescripcion
            };
        }

        string messageBoxText = $"Datos correctos. Quiere guardar los datos del alumno {Apellido}, {Nombre} ({Documento})?";
        string caption = "Registrar Alumno";
        var messageBox = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (messageBox == MessageBoxResult.Yes)
        {
            _servicioAlumno.RegistrarAlumno(informacionPersonal, domicilio, contacto);
            MessageBox.Show(messageBoxText, caption);
        }
    }
}
