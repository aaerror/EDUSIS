using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Requests;
using Core.Shared.DTOs.Personas;
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
    private readonly IServicioDocentes _servicioDocentes;

    private InformacionPersonalDTO _informacionPersonalDTO;
    private ContactoDTO _contactoDTO;
    private DomicilioDTO _domicilioDTO;

    private InformacionPersonalViewModel _informacionPersonalViewModel;
    private ContactoViewModel _contactoViewModel;
    private DomicilioViewModel _domicilioViewModel;
    private DocenteInstitucionalViewModel _docenteInstitucionalViewModel;
    private PuestoDocenteViewModel _puestoDocenteViewModel;

    private int _tab = 0;

    private Dictionary<string, List<string>> _errorsByProperty = new();

    public bool HasErrors => _errorsByProperty.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand AtrasCommand { get; }
    public ViewModelCommand ContinuarCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    #endregion


    public RegistrarDocenteViewModel(IServicioDocentes servicioDocentes)
    {
        _servicioDocentes = servicioDocentes;

        _informacionPersonalViewModel = new(_informacionPersonalDTO);
        _contactoViewModel = new(_contactoDTO);
        _domicilioViewModel = new(_domicilioDTO);
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

    public DocenteInstitucionalViewModel DocenteInstitucionalViewModel
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

    #region AtrasCommand
    private bool CanExecuteAtrasCommand(object obj) => !DocenteInstitucionalViewModel.HasErrors && !PuestoDocenteViewModel.HasErrors;

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

        DocenteInstitucionalViewModel.Documento = InformacionPersonalViewModel.Documento;
        Tab = 1;
    }
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
            caption = "Error al Registrar un Docente";

            MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);

            DocenteInstitucionalViewModel.Legajo = string.Empty;
            DocenteInstitucionalViewModel.PrefijoCuil = string.Empty;
            DocenteInstitucionalViewModel.PosfijoCuil = string.Empty;
        }
        else
        {
            messageBoxText = $"¿Está seguro que desea registrar los datos del docente { InformacionPersonalViewModel.Apellido }, { InformacionPersonalViewModel.Nombre }?";
            caption = "Registrar Docente";
            result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
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

                var requestPuesto = new RegistrarPuestoDocenteRequest(PuestoDocenteViewModel.PosicionIndex, PuestoDocenteViewModel.FechaInicio);

                var request = new RegistrarDocenteRequest(DocenteInstitucionalViewModel.Legajo,
                                                          DocenteInstitucionalViewModel.CUIL,
                                                          requestPuesto,
                                                          requestInformacionPersonal,
                                                          requestContacto,
                                                          requestDomicilio);
                try
                {
                    if (_servicioDocentes.EsDocumentoInvalido(request.InformacionPersonalDTO.Documento))
                    {
                        messageBoxText = $"El número de documento, { request.InformacionPersonalDTO.Documento }, ya se encuentra registrado con otro docente.";
                        caption = "Error";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                        InformacionPersonalViewModel.Documento = string.Empty;
                        Tab = 0;

                        return;
                    }

                    if (_servicioDocentes.EsLegajoInvalido(request.Legajo))
                    {
                        messageBoxText = $"El legajo docente, { request.Legajo }, ya se encuentra registrado con otro docente.";
                        caption = "Error";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

                        DocenteInstitucionalViewModel.Legajo = string.Empty;

                        return;
                    }

                    if (_servicioDocentes.EsCuilInvalido(request.CUIL))
                    {
                        messageBoxText = $"El CUIL del docente, { request.InformacionPersonalDTO.Apellido } {request.InformacionPersonalDTO.Nombre }, ya se encuentra registrado con otro personal de la institución.";
                        caption = "Error";
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
                    DocenteInstitucionalViewModel = new DocenteInstitucionalViewModel(null);
                    PuestoDocenteViewModel = new PuestoDocenteViewModel(null);

                    Tab = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    #endregion
}
