using Core.ServicioAlumnos;
using Core.ServicioAlumnos.DTOs.Requests;
using Core.ServicioAlumnos.DTOs.Responses;
using System;
using System.Windows;
using System.Windows.Input;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Alumnos;

public class PerfilAlumnoViewModel : ViewModel
{
    private IServicioAlumnos _servicioAlumno;

    private PersonaConDetallesResponse _personaConDetalles;
    private DomicilioRequest _datosDomicilioTemporal;
    private ContactoRequest _datosContactoTemporal;
    private CambiarSexoRequest _datosSexoTemporal;

    private PerfilBuscadoStore _perfilBuscadoStore;

    private bool _habilitarEditarContacto = false;
    private bool _habilitarEditarDomicilio = false;
    private bool _habilitarEditarSexo = false;

    #region Commands
    public ViewModelCommand CancelarEditarCommand { get; }
    public ViewModelCommand EditarCommand { get; }
    public ViewModelCommand GuardarCambiosCommand { get; }
    #endregion


    public PerfilAlumnoViewModel(IServicioAlumnos servicioAlumno, PerfilBuscadoStore perfilBuscadoStore)
    {
        _servicioAlumno = servicioAlumno;
        _perfilBuscadoStore = perfilBuscadoStore;

        if (string.IsNullOrWhiteSpace(_perfilBuscadoStore.Documento))
        {
            throw new ArgumentNullException(nameof(perfilBuscadoStore.Documento), "Error al ver el perfil del alumno. Primero debe buscar un alumno antes de continuar.");
        }

        EditarCommand = new ViewModelCommand(EditarCommandExecute, EditarCommandCanExecute);
        CancelarEditarCommand = new ViewModelCommand(CancelarEditarCommandExecute, CancelarEditarCommandCanExecute);
        GuardarCambiosCommand = new ViewModelCommand(GuardarCambiosCommandExecute, GuardarCambiosCommandCanExecute);

        _personaConDetalles = _servicioAlumno.BuscarPorDNIConDetalles(_perfilBuscadoStore.Documento);

        _datosDomicilioTemporal = new DomicilioRequest(_personaConDetalles.Calle,
                                                       _personaConDetalles.Altura,
                                                       _personaConDetalles.Vivienda,
                                                       _personaConDetalles.Observacion,
                                                       _personaConDetalles.Localidad,
                                                       _personaConDetalles.Provincia,
                                                       _personaConDetalles.Pais);
        _datosContactoTemporal = new ContactoRequest(_personaConDetalles.Email, _personaConDetalles.Telefono);
        _datosSexoTemporal = new CambiarSexoRequest(_personaConDetalles.Apellido, _personaConDetalles.Nombre, _personaConDetalles.Sexo);
    }

    #region Properties
    public PersonaConDetallesResponse PersonaConDetalles
    {
        get
        {
            return _personaConDetalles;
        }

        set
        {
            _personaConDetalles = value;
            OnPropertyChanged(nameof(PersonaConDetalles));
        }
    }

    public bool HabilitarEditarContacto
    {
        get
        {
            return _habilitarEditarContacto;
        }

        set
        {
            _habilitarEditarContacto = value;
            OnPropertyChanged(nameof(HabilitarEditarContacto));
        }
    }

    public bool HabilitarEditarDomicilio
    {
        get
        {
            return _habilitarEditarDomicilio;
        }

        set
        {
            _habilitarEditarDomicilio = value;
            OnPropertyChanged(nameof(HabilitarEditarDomicilio));
        }
    }

    public bool HabilitarEditarSexo
    {
        get
        {
            return _habilitarEditarSexo;
        }

        set
        {
            _habilitarEditarSexo = value;
            OnPropertyChanged(nameof(HabilitarEditarSexo));
        }
    }
    #endregion

    #region CancelarCommand
    private bool CancelarEditarCommandCanExecute(object obj)
    {
        switch (obj as string)
        {
            case "Contacto":
                return HabilitarEditarContacto;
            case "Domicilio":
                return HabilitarEditarDomicilio;
            case "Sexo":
                return HabilitarEditarSexo;
            default:
                return false;
        }
    }

    private void CancelarEditarCommandExecute(object obj)
    {
        switch (obj as string)
        {
            case "Contacto":
                PersonaConDetalles.Email = _datosContactoTemporal.Email;
                PersonaConDetalles.Telefono = _datosContactoTemporal.Telefono;
                OnPropertyChanged(nameof(PersonaConDetalles));

                HabilitarEditarContacto = false;

                break;
            case "Domicilio":
                PersonaConDetalles.Calle = _datosDomicilioTemporal.Calle;
                PersonaConDetalles.Altura = _datosDomicilioTemporal.Altura;
                PersonaConDetalles.Vivienda = _datosDomicilioTemporal.Vivienda;
                PersonaConDetalles.Observacion = _datosDomicilioTemporal.Observacion;
                PersonaConDetalles.Localidad = _datosDomicilioTemporal.Localidad;
                PersonaConDetalles.Provincia = _datosDomicilioTemporal.Provincia;
                PersonaConDetalles.Pais = _datosDomicilioTemporal.Pais;
                OnPropertyChanged(nameof(PersonaConDetalles));

                HabilitarEditarDomicilio = false;

                break;
            case "Sexo":
                PersonaConDetalles.Apellido = _datosSexoTemporal.Apellido;
                PersonaConDetalles.Nombre = _datosSexoTemporal.Nombre;
                PersonaConDetalles.Sexo = _datosSexoTemporal.Sexo;
                OnPropertyChanged(nameof(PersonaConDetalles));

                HabilitarEditarSexo = false;

                break;
            default: return;
        }
    }
    #endregion

    #region EditarCommand
    private bool EditarCommandCanExecute(object obj)
    {
        switch (obj as string)
        {
            case "Contacto":
                return !HabilitarEditarContacto;
            case "Domicilio":
                return !HabilitarEditarDomicilio;
            case "Sexo":
                return !HabilitarEditarSexo;
            default:
                return false;
        }
    }

    private void EditarCommandExecute(object obj)
    {
        switch (obj as string)
        {
            case "Contacto":
                HabilitarEditarContacto = true;
                break;
            case "Domicilio":
                HabilitarEditarDomicilio = true;
                break;
            case "Sexo":
                HabilitarEditarSexo = true;
                break;
            default:
                MessageBox.Show("Error al habilitar la edición.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                break;
        }
    }
    #endregion

    #region GuardarCambiosCommand
    private bool GuardarCambiosCommandCanExecute(object obj)
    {
        switch (obj as string)
        {
            case "Contacto":
                return HabilitarEditarContacto;
            case "Domicilio":
                return HabilitarEditarDomicilio;
            case "Sexo":
                return HabilitarEditarSexo;
            default:
                return false;
        }
    }

    private void GuardarCambiosCommandExecute(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Contacto":
                var nuevosDatosContacto = new ContactoRequest(PersonaConDetalles.Email, PersonaConDetalles.Telefono);

                messageBoxText = $"Se van a registrar los siguientes datos de cambio de contacto:\n" +
                                 $"Email: {PersonaConDetalles.Email}\n" +
                                 $"Teléfono: {PersonaConDetalles.Telefono}\n\n" +
                                 $"¿Desea continuar?";
                caption = "Actualización de Contacto";

                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _servicioAlumno.ModificarContacto(PersonaConDetalles.PersonaId, nuevosDatosContacto);
                        MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        HabilitarEditarContacto = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                break;

            case "Domicilio":
                var nuevoDomicilio = new DomicilioRequest(PersonaConDetalles.Calle,
                                                          PersonaConDetalles.Altura,
                                                          PersonaConDetalles.Vivienda,
                                                          PersonaConDetalles.Observacion,
                                                          PersonaConDetalles.Localidad,
                                                          PersonaConDetalles.Provincia,
                                                          PersonaConDetalles.Pais);

                messageBoxText = $"Se van a registrar los siguientes datos de cambio de domicilio:\n" +
                                 $"Calle con Altura: {PersonaConDetalles.Calle} {PersonaConDetalles.Altura}\n" +
                                 $"Vivienda: {PersonaConDetalles.Vivienda}\n" +
                                 $"Observaciones: {PersonaConDetalles.Observacion}\n" +
                                 $"Localidad: {PersonaConDetalles.Localidad}, {PersonaConDetalles.Provincia}, {PersonaConDetalles.Pais}";
                caption = "Actualizar domicilio";

                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _servicioAlumno.ModificarDomicilio(PersonaConDetalles.PersonaId, nuevoDomicilio);
                        MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        HabilitarEditarDomicilio = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                break;

            case "Sexo":
                var nuevosDatosCambioSexo = new CambiarSexoRequest(PersonaConDetalles.Apellido, PersonaConDetalles.Nombre, PersonaConDetalles.Sexo);

                messageBoxText = $"Se van a registrar los siguientes datos de cambio de sexo del alumno:\n" +
                                 $"Nombre Comple: {PersonaConDetalles.Apellido}, {PersonaConDetalles.Nombre}\n" +
                                 $"Sexo: {PersonaConDetalles.Sexo}\n\n" +
                                 $"¿Desea continuar?";
                caption = "Cambio de Sexo";

                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _servicioAlumno.ModificarSexo(PersonaConDetalles.PersonaId, nuevosDatosCambioSexo);
                        MessageBox.Show("Datos guardados correctamente", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        HabilitarEditarSexo = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                break;
        }
    }
    #endregion
}
