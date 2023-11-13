using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Responses;
using Core.Shared.DTOs.Personas.Requests;
using Domain.Personas;
using Domain.Personas.Domicilios;
using System;
using System.Windows;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Docentes;

public class PerfilDocenteViewModel : ViewModel
{
    private readonly IServicioDocentes _servicioDocentes;
    private PerfilBuscadoStore _perfilBuscadoStore;

    private readonly DocenteConDetalleResponse _docenteTemporal = null;
    private DocenteConDetalleResponse _docente = null;
    private CambiarSexoRequest _cambiarSexoRequest = null;
    private CambiarContactoRequest _cambiarContactoRequest = null;
    private CambiarDomicilioRequest _cambiarDomicilioRequest = null;

    private bool _habilitarEditarContacto = false;
    private bool _habilitarEditarDomicilio = false;
    private bool _habilitarEditarSexo = false;

    #region Commands
    public ViewModelCommand CancelarEditarCommand { get; }
    public ViewModelCommand EditarCommand { get; }
    public ViewModelCommand GuardarCambiosCommand { get; }
    #endregion


    public PerfilDocenteViewModel(IServicioDocentes servicioDocentes, PerfilBuscadoStore perfilBuscadoStore)
    {
        _servicioDocentes = servicioDocentes;
        _perfilBuscadoStore = perfilBuscadoStore;

        if (string.IsNullOrWhiteSpace(_perfilBuscadoStore.Documento))
        {
            throw new ArgumentNullException(nameof(perfilBuscadoStore.Documento), "Error al ver el perfil del docente. Primero debe buscar un docente antes de continuar.");
        }

        _docenteTemporal = _servicioDocentes.BuscarDocenteConDetalle(_perfilBuscadoStore.PersonaID);
        Docente = _docenteTemporal;

        EditarCommand = new ViewModelCommand(EditarCommandExecute, EditarCommandCanExecute);
        CancelarEditarCommand = new ViewModelCommand(CancelarEditarCommandExecute, CancelarEditarCommandCanExecute);
        GuardarCambiosCommand = new ViewModelCommand(GuardarCambiosCommandExecute, GuardarCambiosCommandCanExecute);
    }

    #region Properties
    public DocenteConDetalleResponse Docente
    {
        get
        {
            return _docente;
        }

        set
        {
            _docente = value;
            OnPropertyChanged(nameof(Docente));
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
                Docente = _docenteTemporal;
                HabilitarEditarContacto = false;
                break;
            case "Domicilio":
                Docente = _docenteTemporal;
                HabilitarEditarDomicilio = false;
                break;
            case "Sexo":
                Docente = _docenteTemporal;
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
                messageBoxText = $"Se van a registrar los siguientes datos de cambio de contacto:\n" +
                                 $"Email: { Docente.ContactoDTO.Email }\n" +
                                 $"Teléfono: { Docente.ContactoDTO.Telefono}\n\n" +
                                 $"¿Desea continuar?";
                caption = "Actualización de Contacto";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _cambiarContactoRequest = new CambiarContactoRequest(Docente.DocenteID,
                                                                             Docente.ContactoDTO.Telefono,
                                                                             Docente.ContactoDTO.Email);
                        _servicioDocentes.ModificarContacto(_cambiarContactoRequest);

                        MessageBox.Show("¡Datos actualizados correctamente!", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        HabilitarEditarContacto = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                break;
            case "Domicilio":
                messageBoxText = $"Se van a registrar los siguientes datos de cambio de domicilio:\n" +
                                 $"Calle con Altura: { Docente.DomicilioDTO.Calle } { Docente.DomicilioDTO.Altura } ({ (Vivienda) Docente.DomicilioDTO.Vivienda })\n" +
                                 $"Observaciones: {Docente.DomicilioDTO.Observacion }\n" +
                                 $"Localidad: { Docente.DomicilioDTO.Localidad }, {Docente.DomicilioDTO.Provincia }, { Docente.DomicilioDTO.Pais }";
                caption = "Actualización de Domicilio";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _cambiarDomicilioRequest = new CambiarDomicilioRequest(Docente.DocenteID,
                                                                               Docente.DomicilioDTO.Calle,
                                                                               Docente.DomicilioDTO.Altura,
                                                                               Docente.DomicilioDTO.Vivienda,
                                                                               Docente.DomicilioDTO.Observacion,
                                                                               Docente.DomicilioDTO.Localidad,
                                                                               Docente.DomicilioDTO.Provincia,
                                                                               Docente.DomicilioDTO.Pais);
                        _servicioDocentes.ModificarDomicilio(_cambiarDomicilioRequest);

                        MessageBox.Show("¡Datos actualizados correctamente!", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        HabilitarEditarDomicilio = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                break;
            case "Sexo":
                messageBoxText = $"Se van a registrar los siguientes datos de cambio de sexo del docente:\n" +
                                 $"Nombre Completo: { Docente.InformacionPersonalDTO.Apellido }, {Docente.InformacionPersonalDTO.Nombre }\n" +
                                 $"Sexo: { (Sexo) Docente.InformacionPersonalDTO.Sexo }\n\n" +
                                 $"¿Desea continuar?";
                caption = "Cambio de Sexo";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _cambiarSexoRequest = new CambiarSexoRequest(Docente.DocenteID,
                                                                     Docente.InformacionPersonalDTO.Apellido,
                                                                     Docente.InformacionPersonalDTO.Nombre,
                                                                     Docente.InformacionPersonalDTO.Sexo);
                        _servicioDocentes.ModificarSexo(_cambiarSexoRequest);

                        MessageBox.Show("¡Datos actualizados correctamente!", "Operación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
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
