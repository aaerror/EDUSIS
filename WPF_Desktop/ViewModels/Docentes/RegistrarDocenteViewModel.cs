using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Requests;
using Core.Shared.DTOs.Personas.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Shared;
using WPF_Desktop.ViewModels.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

public class RegistrarDocenteViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioDocente _servicioDocentes;

    #region Requests
    private RegistrarDocenteRequest _registrarDocenteRequest;
    #endregion

    #region ViewModel
    private InformacionPersonalViewModel _informacionPersonalViewModel;
    private ContactoViewModel _contactoViewModel;
    private DomicilioViewModel _domicilioViewModel;
    private LegajoDocenteViewModel _docenteInstitucionalViewModel;
    private PuestoDocenteViewModel _puestoDocenteViewModel;
    #endregion

    private int _tab = 0;

    private Dictionary<string, List<string>> _errorsByProperty = new();

    public bool HasErrors => _errorsByProperty.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand ContinuarCommand { get; }
    public ViewModelCommand AtrasCommand { get; }
    #endregion


    public RegistrarDocenteViewModel(IServicioDocente servicioDocentes)
    {
        _servicioDocentes = servicioDocentes;

        _informacionPersonalViewModel = new(null);
        _contactoViewModel = new(null);
        _domicilioViewModel = new(null);
        _docenteInstitucionalViewModel = new(null);
        _puestoDocenteViewModel = new(null);

        AtrasCommand = new ViewModelCommand(ExecuteAtrasCommand, CanExecuteAtrasCommand);
        ContinuarCommand = new ViewModelCommand(ExecuteContinuarCommand, CanExecuteContinuarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
    }

    private bool CUILInvalido(string prefijo, string documento, string posfijo)
    {
        string cuil = prefijo + documento + posfijo;

        return _servicioDocentes.EsCuilInvalido(cuil);
    }

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

    #region Properties

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

    public LegajoDocenteViewModel DocenteInstitucionalViewModel
    {
        get
        {
            return _docenteInstitucionalViewModel;
        }

        set
        {
            _docenteInstitucionalViewModel = value;
            OnPropertyChanged(nameof(DocenteInstitucionalViewModel));
        }
    }

    public PuestoDocenteViewModel PuestoDocenteViewModel
    {
        get
        {
            return _puestoDocenteViewModel;
        }

        set
        {
            _puestoDocenteViewModel = value;
            OnPropertyChanged(nameof(PuestoDocenteViewModel));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj) => !HasErrors && !InformacionPersonalViewModel.HasErrors && !DomicilioViewModel.HasErrors && !ContactoViewModel.HasErrors;

    private void ExecuteGuardarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (string.IsNullOrWhiteSpace(DocenteInstitucionalViewModel.Legajo) || string.IsNullOrWhiteSpace(DocenteInstitucionalViewModel.PrefijoCuil) || string.IsNullOrWhiteSpace(DocenteInstitucionalViewModel.PosfijoCuil))
        {
            messageBoxText = "Se deben ingresar los datos correspondientes para continuar.";
            caption = "Error en la operación";

            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            DocenteInstitucionalViewModel.Legajo = string.Empty;
            DocenteInstitucionalViewModel.PrefijoCuil = string.Empty;
            DocenteInstitucionalViewModel.PosfijoCuil = string.Empty;
        }
        else
        {
            messageBoxText = $"¿Está seguro que desea registrar los datos del docente {InformacionPersonalViewModel.Apellido}, {InformacionPersonalViewModel.Nombre}?";
            caption = "Registrar Docente";
            result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result is MessageBoxResult.Yes)
            {
                var requestPuesto = new RegistrarPuestoDocenteRequest(PuestoDocenteViewModel.Posicion, PuestoDocenteViewModel.FechaInicio);

                var request = new RegistrarDocenteRequest(
                    Legajo: DocenteInstitucionalViewModel.Legajo,
                    CUIL: DocenteInstitucionalViewModel.CUIL,
                    FechaAlta: DocenteInstitucionalViewModel.FechaAlta,
                    Puesto: requestPuesto,
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
                try
                {
                    if (_servicioDocentes.EsDocumentoInvalido(request.DNI))
                    {
                        messageBoxText = $"El número de documento, {request.DNI}, ya se encuentra registrado con otro docente.";
                        caption = "Error en la operación";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                        InformacionPersonalViewModel.DNI = string.Empty;
                        Tab = 0;

                        return;
                    }

                    if (_servicioDocentes.EsLegajoInvalido(request.Legajo))
                    {
                        messageBoxText = $"El legajo docente, {request.Legajo}, ya se encuentra registrado con otro docente.";
                        caption = "Error en la operación";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                        DocenteInstitucionalViewModel.Legajo = string.Empty;

                        return;
                    }

                    if (_servicioDocentes.EsCuilInvalido(request.CUIL))
                    {
                        messageBoxText = $"El CUIL del docente, {request.Apellido} {request.Nombre}, ya se encuentra registrado con otro personal de la institución.";
                        caption = "Error en la operación";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                        DocenteInstitucionalViewModel.PrefijoCuil = string.Empty;
                        DocenteInstitucionalViewModel.PosfijoCuil = string.Empty;

                        return;
                    }

                    _servicioDocentes.RegistrarDocente(request);

                    messageBoxText = $"Se han registrado los datos de un nuevo docente.";
                    caption = "Operación Exitosa";
                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                    InformacionPersonalViewModel = new InformacionPersonalViewModel(null);
                    DomicilioViewModel = new DomicilioViewModel(null);
                    ContactoViewModel = new ContactoViewModel(null);
                    DocenteInstitucionalViewModel = new LegajoDocenteViewModel(null);
                    PuestoDocenteViewModel = new PuestoDocenteViewModel(null);

                    Tab = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    #endregion

    #region ContinuarCommand
    private bool CanExecuteContinuarCommand(object obj) => !InformacionPersonalViewModel.HasErrors && !DomicilioViewModel.HasErrors && !ContactoViewModel.HasErrors;

    private void ExecuteContinuarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        if (string.IsNullOrWhiteSpace(InformacionPersonalViewModel.Apellido) || string.IsNullOrWhiteSpace(InformacionPersonalViewModel.Nombre) || string.IsNullOrWhiteSpace(InformacionPersonalViewModel.DNI) || string.IsNullOrWhiteSpace(DomicilioViewModel.Calle) || string.IsNullOrWhiteSpace(DomicilioViewModel.Localidad) || string.IsNullOrWhiteSpace(DomicilioViewModel.Provincia) || string.IsNullOrWhiteSpace(ContactoViewModel.Telefono) || string.IsNullOrWhiteSpace(ContactoViewModel.Email))
        {
            messageBoxText = "Se deben ingresar los datos correspondientes para continuar.";
            caption = "Error en la operación";

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

        DocenteInstitucionalViewModel.NombreCompleto = InformacionPersonalViewModel.Apellido + ", " + InformacionPersonalViewModel.Nombre;
        DocenteInstitucionalViewModel.DNI = InformacionPersonalViewModel.DNI;
        Tab = 1;
    }
    #endregion

    #region AtrasCommand
    private bool CanExecuteAtrasCommand(object obj) => !DocenteInstitucionalViewModel.HasErrors && !PuestoDocenteViewModel.HasErrors;

    private void ExecuteAtrasCommand(object obj)
    {
        Tab = 0;
    }
    #endregion
}
