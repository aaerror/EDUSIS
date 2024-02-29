using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.Shared.DTOs.Personas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels.Shared;
using Core.ServicioCursos;
using System.Collections.ObjectModel;
using WPF_Desktop.ViewModels.Cursos;
using WPF_Desktop.ViewModels.Cursos.Divisiones;
using System.Text.RegularExpressions;
using Core.ServicioCursos.DTOs.Requests;

namespace WPF_Desktop.ViewModels.Alumnos;

public class RegistrarAlumnoViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioAlumnos _servicioAlumnos;
    private readonly IServicioCursos _servicioCursos;

    #region Request
    private CrearCursanteRequest _crearCursanteRequest;
    #endregion

    private Guid alumnoID = Guid.Empty;
    private InformacionPersonalDTO _informacionPersonalDTO;
    private ContactoDTO _contactoDTO;
    private DomicilioDTO _domicilioDTO;

    private string _periodo;
    private InformacionPersonalViewModel _informacionPersonalViewModel;
    private DomicilioViewModel _domicilioViewModel;
    private ContactoViewModel _contactoViewModel;

    private CursoViewModel _cursoViewModel;
    private DivisionViewModel _divisionViewModel;

    /*
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
        private string _observaciones = string.Empty;
        private string _localidad = string.Empty;
        private string _provincia = string.Empty;
        private string _pais = "Argentina";
    */

    private int _tab = 0;

    private Dictionary<string, List<string>> _errorsByProperty = new();

    public bool HasErrors => _errorsByProperty.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    private ObservableCollection<CursoViewModel> _cursos = new();
    private ObservableCollection<DivisionViewModel> _divisiones = new();

    #region Command
    public ViewModelCommand AtrasCommand { get; }
    public ViewModelCommand ContinuarCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    #endregion


    public RegistrarAlumnoViewModel(IServicioAlumnos servicioAlumnos, IServicioCursos servicioCursos)
    {
        _servicioAlumnos = servicioAlumnos;
        _servicioCursos = servicioCursos;

        _informacionPersonalViewModel = new(_informacionPersonalDTO);
        _domicilioViewModel = new(_domicilioDTO);
        _contactoViewModel = new(_contactoDTO);

        ActualizarCursos();

        AtrasCommand = new ViewModelCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
        ContinuarCommand = new ViewModelCommand(ExecuteContinuarCommand, CanExecuteContinuarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
    }

    private void ActualizarCursos()
    {
        var cursos = _servicioCursos.BuscarCursos();
        _cursos = new ObservableCollection<CursoViewModel>(cursos.Select(x => new CursoViewModel(x)));
    }

    #region Properties
    public int Tab
    {
        get
        {
            return _tab;
        }

        set
        {
            _tab = value;
            OnPropertyChanged(nameof(Tab));
        }
    }

    public string Periodo
    {
        get
        {
            return _periodo;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Periodo));
            _periodo = value;
            OnPropertyChanged(nameof(Periodo));

            if (string.IsNullOrWhiteSpace(Periodo))
            {
                _errorsByProperty.Add(nameof(Periodo), new List<string>
                {
                    "Debe ingresar el año en el que desea inscribir el alumno."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Periodo)));
            }
            else
            {
                if (!Regex.IsMatch(Periodo, @"^(20)\d{2}$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Periodo), new List<string>
                    {
                        "Formato inválido del año. Puede ingresar un año comprendido entre el 2000 y el 2099."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Periodo)));
                }
            }
        }
    }

    public InformacionPersonalViewModel InformacionPersonalViewModel
    {
        get
        {
            return _informacionPersonalViewModel;
        }

        set
        {
            _informacionPersonalViewModel = value;
            OnPropertyChanged(nameof(InformacionPersonalViewModel));
        }
    }

    public DomicilioViewModel DomicilioViewModel
    {
        get
        {
            return _domicilioViewModel;
        }

        set
        {
            _domicilioViewModel = value;
            OnPropertyChanged(nameof(DomicilioViewModel));
        }
    }

    public ContactoViewModel ContactoViewModel
    {
        get
        {
            return _contactoViewModel;
        }

        set
        {
            _contactoViewModel = value;
            OnPropertyChanged(nameof(ContactoViewModel));
        }
    }

    public CursoViewModel CursoViewModel
    {
        get
        {
            return _cursoViewModel;
        }

        set
        {
            _cursoViewModel = value;
            OnPropertyChanged(nameof(CursoViewModel));

            if (CursoViewModel is not null)
            {
                var divisiones = _servicioCursos.BuscarDivisiones(CursoViewModel.CursoID);
                Divisiones = new ObservableCollection<DivisionViewModel>(divisiones.Select(x => new DivisionViewModel(x)));
            }
            
        }
    }

    public DivisionViewModel DivisionViewModel
    {
        get
        {
            return _divisionViewModel;
        }

        set
        {
            _divisionViewModel = value;
            OnPropertyChanged(nameof(DivisionViewModel));
        }
    }

    public ObservableCollection<CursoViewModel> Cursos
    {
        get
        {
            return _cursos;
        }
    }

    public ObservableCollection<DivisionViewModel> Divisiones
    {
        get
        {
            return _divisiones;
        }

        set
        {
            _divisiones = value;
            OnPropertyChanged(nameof(Divisiones));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion

    #region AtrasCommand
    private bool CanExecuteAtrasCommand(object obj)
    {
        return true;
    }

    private void ExecuteAtrasCommand(object obj)
    {
        Tab = 0;
    }
    #endregion

    #region ContinuarCommand
    private bool CanExecuteContinuarCommand(object obj) => !InformacionPersonalViewModel.HasErrors && !DomicilioViewModel.HasErrors && !ContactoViewModel.HasErrors;

    private void ExecuteContinuarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (string.IsNullOrWhiteSpace(InformacionPersonalViewModel.Apellido) || string.IsNullOrWhiteSpace(InformacionPersonalViewModel.Nombre) || string.IsNullOrWhiteSpace(InformacionPersonalViewModel.Documento) || string.IsNullOrWhiteSpace(DomicilioViewModel.Calle) ||
            string.IsNullOrWhiteSpace(DomicilioViewModel.Localidad) || string.IsNullOrWhiteSpace(DomicilioViewModel.Provincia) || string.IsNullOrWhiteSpace(ContactoViewModel.Telefono) || string.IsNullOrWhiteSpace(ContactoViewModel.Email))
        {
            messageBoxText = "Se deben ingresar los datos correspondientes para continuar.";
            caption = "Error al Registrar un Docente";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            InformacionPersonalViewModel = new InformacionPersonalViewModel(new InformacionPersonalDTO(string.Empty,
                                                                                                       string.Empty,
                                                                                                       string.Empty,
                                                                                                       0,
                                                                                                       DateTime.Now,
                                                                                                       string.Empty));
            DomicilioViewModel = new DomicilioViewModel(new DomicilioDTO(string.Empty,
                                                                         string.Empty,
                                                                         0,
                                                                         string.Empty,
                                                                         string.Empty,
                                                                         string.Empty,
                                                                         string.Empty));
            ContactoViewModel = new ContactoViewModel(new ContactoDTO(string.Empty, string.Empty));

            return;
        }
        else
        {
            messageBoxText = $"¿Está seguro que desea registrar los datos del alumno {InformacionPersonalViewModel.Apellido}, {InformacionPersonalViewModel.Nombre}?";
            caption = "Registrar Alumno";
            result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result is MessageBoxResult.Yes)
            {
                var requestInformacionPersonal = new InformacionPersonalDTO(InformacionPersonalViewModel.Apellido,
                                                                            InformacionPersonalViewModel.Nombre,
                                                                            InformacionPersonalViewModel.Documento,
                                                                            InformacionPersonalViewModel.Sexo,
                                                                            InformacionPersonalViewModel.FechaNacimiento,
                                                                            InformacionPersonalViewModel.Nacionalidad);

                var requestDomicilio = new DomicilioDTO(DomicilioViewModel.Calle,
                                                        DomicilioViewModel.Altura,
                                                        DomicilioViewModel.Vivienda,
                                                        DomicilioViewModel.Observaciones,
                                                        DomicilioViewModel.Localidad,
                                                        DomicilioViewModel.Provincia,
                                                        DomicilioViewModel.Pais);

                var requestContacto = new ContactoDTO(ContactoViewModel.Telefono, ContactoViewModel.Email);

                try
                {
                    if (_servicioAlumnos.EsDocumentoInvalido(requestInformacionPersonal.Documento))
                    {
                        messageBoxText = $"El número de documento ({requestInformacionPersonal.Documento}) ya se encuentra registrado con otra persona.";
                        caption = "Error";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                        InformacionPersonalViewModel.Documento = string.Empty;

                        return;
                    }

                    var request = new RegistrarAlumnoRequest(requestInformacionPersonal, requestDomicilio, requestContacto);
                    alumnoID = _servicioAlumnos.RegistrarAlumno(request);

                    messageBoxText = $"Se han registrado los datos de un nuevo alumno.";
                    caption = "Operación Exitosa";
                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                    Tab = 1;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj) => !HasErrors && CursoViewModel is not null && DivisionViewModel is not null;

    private void ExecuteGuardarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (string.IsNullOrWhiteSpace(Periodo))
        {
            messageBoxText = "Se deben completar todos los campos necesarios para poder registrar el alumno en el curso.";
            caption = "Error";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            Periodo = string.Empty;

            return;
        }

        if (alumnoID.Equals(Guid.Empty))
        {
            messageBoxText = "Antes de inscribir el alumno en el curso debe estar registrado previamente.";
            caption = "Error";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            Tab = 0;
        }

        messageBoxText = $"Está a punto de inscribir a { InformacionPersonalViewModel.Apellido } { InformacionPersonalViewModel.Nombre } en:\n" +
                         $"\tCurso: { CursoViewModel.Descripcion }° Año ({ CursoViewModel.NivelEducativo })\n"+
                         $"\tDivisión: { DivisionViewModel.DivisionDescripcion }\n"+
                         $"\tCiclo Lectivo: { Periodo }\n\n"+
                         $"¿Desea continuar?";
        caption = "Inscripción del Alumno";
        result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result is MessageBoxResult.Yes)
        {
            try
            {
                _crearCursanteRequest = new CrearCursanteRequest(CursoViewModel.CursoID, DivisionViewModel.DivisionID, alumnoID, Periodo);
                _servicioCursos.InscribirAlumnoEnDivision(_crearCursanteRequest);

                messageBoxText = $"Se han inscripto el alumno en el curso correctamente .";
                caption = "Operación Exitosa";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                alumnoID = Guid.Empty;
                InformacionPersonalViewModel = new(null);
                DomicilioViewModel = new(null);
                ContactoViewModel = new(null);

                ActualizarCursos();

                Tab = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion
}
