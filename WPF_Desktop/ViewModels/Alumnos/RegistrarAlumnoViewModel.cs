using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
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
using Core.Shared.DTOs.Personas.Requests;
using Core.Shared.DTOs.Personas.Responses;

namespace WPF_Desktop.ViewModels.Alumnos;

public class RegistrarAlumnoViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioAlumno _servicioAlumnos;
    private readonly IServicioCurso _servicioCursos;

    #region Request
    private CrearCursanteRequest _crearCursanteRequest;
    private RegistrarInformacionPersonalRequest _informacionPersonalRequest;
    private RegistrarContactoRequest _contactoRequest;
    private RegistrarDomicilioRequest _domicilioRequest;
    #endregion

    #region ViewModel
    private InformacionPersonalViewModel _informacionPersonalViewModel;
    private DomicilioViewModel _domicilioViewModel;
    private ContactoViewModel _contactoViewModel;
    private CursoViewModel _cursoViewModel;
    private DivisionViewModel _divisionViewModel;
    #endregion

    private Guid alumnoID = Guid.Empty;
    private string _periodo;

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


    public RegistrarAlumnoViewModel(IServicioAlumno servicioAlumnos, IServicioCurso servicioCursos)
    {
        _servicioAlumnos = servicioAlumnos;
        _servicioCursos = servicioCursos;

        _informacionPersonalViewModel = new(null);
        _domicilioViewModel = new(null);
        _contactoViewModel = new(null);

        ActualizarCursos();

        AtrasCommand = new ViewModelCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
        ContinuarCommand = new ViewModelCommand(ExecuteContinuarCommand, CanExecuteContinuarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
    }

    private void ActualizarCursos()
    {
        var cursos = _servicioCursos.ListarCursos();
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

    private async void ExecuteContinuarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (string.IsNullOrWhiteSpace(InformacionPersonalViewModel.Apellido) || string.IsNullOrWhiteSpace(InformacionPersonalViewModel.Nombre) || string.IsNullOrWhiteSpace(InformacionPersonalViewModel.DNI) || string.IsNullOrWhiteSpace(DomicilioViewModel.Calle) || string.IsNullOrWhiteSpace(DomicilioViewModel.Localidad) || string.IsNullOrWhiteSpace(DomicilioViewModel.Provincia) || string.IsNullOrWhiteSpace(ContactoViewModel.Telefono) || string.IsNullOrWhiteSpace(ContactoViewModel.Email))
        {
            messageBoxText = "Se deben ingresar los datos correspondientes para continuar.";
            caption = "Error al Registrar un Docente";
            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            InformacionPersonalViewModel = new InformacionPersonalViewModel(
                new InformacionPersonalResponse(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    0,
                    DateTime.Now,
                    string.Empty));

            DomicilioViewModel = new DomicilioViewModel(
                new DomicilioResponse(
                    string.Empty,
                    string.Empty,
                    0,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty));

            ContactoViewModel = new ContactoViewModel(
                new ContactoResponse(string.Empty, string.Empty));

            return;
        }
        else
        {
            messageBoxText = $"¿Está seguro que desea registrar los datos del alumno { InformacionPersonalViewModel.Apellido }, { InformacionPersonalViewModel.Nombre }?";
            caption = "Registrar Alumno";
            result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result is MessageBoxResult.Yes)
            {
                var requestInformacionPersonal = new RegistrarInformacionPersonalRequest(
                    InformacionPersonalViewModel.Apellido,
                    InformacionPersonalViewModel.Nombre,
                    InformacionPersonalViewModel.DNI,
                    InformacionPersonalViewModel.Sexo,
                    InformacionPersonalViewModel.FechaNacimiento,
                    InformacionPersonalViewModel.Nacionalidad);

                var requestDomicilio = new RegistrarDomicilioRequest(
                    DomicilioViewModel.Calle,
                    DomicilioViewModel.Altura,
                    DomicilioViewModel.Vivienda,
                    DomicilioViewModel.Observaciones,
                    DomicilioViewModel.Localidad,
                    DomicilioViewModel.Provincia,
                    DomicilioViewModel.Pais);

                var requestContacto = new RegistrarContactoRequest(ContactoViewModel.Telefono, ContactoViewModel.Email);

                try
                {
                    if (_servicioAlumnos.EsDocumentoInvalido(requestInformacionPersonal.Documento))
                    {
                        messageBoxText = $"El número de documento ({ requestInformacionPersonal.Documento }) ya se encuentra registrado con otra persona.";
                        caption = "Error";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                        InformacionPersonalViewModel.DNI = string.Empty;

                        return;
                    }

                    var request = new RegistrarAlumnoRequest(
                        Apellido: InformacionPersonalViewModel.Apellido,
                        Nombre: InformacionPersonalViewModel.Nombre,
                        DNI: InformacionPersonalViewModel.DNI,
                        Sexo: InformacionPersonalViewModel.Sexo,
                        FechaNacimiento: InformacionPersonalViewModel.FechaNacimiento,
                        Nacionalidad: InformacionPersonalViewModel.Nacionalidad,
                        Telefono: ContactoViewModel.Telefono,
                        Email: ContactoViewModel.Email,
                        Calle: DomicilioViewModel.Calle,
                        Altura: DomicilioViewModel.Altura,
                        Vivienda: DomicilioViewModel.Vivienda,
                        Observacion: DomicilioViewModel.Observaciones,
                        Localidad: DomicilioViewModel.Localidad,
                        Provincia: DomicilioViewModel.Provincia,
                        Pais: DomicilioViewModel.Pais);
                    alumnoID = await _servicioAlumnos.RegistrarAlumnoAsync(request);

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
                         $"\tCurso: { CursoViewModel.GradoDescripcion }° Año ({ CursoViewModel.NivelEducativo })\n"+
                         $"\tDivisión: { DivisionViewModel.Descripcion }\n"+
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
