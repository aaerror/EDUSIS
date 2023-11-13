using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Domain.Docentes.Licencias;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Docentes;

public class GestionLicenciasViewModel : ViewModel
{
    private IServicioDocentes _servicioDocentes;
    private PerfilBuscadoStore _perfilBuscadoStore;

    private bool _habilitarLicenciaViewModel = false;
    private bool _habilitarEdicion = false;
    private bool _habilitarRegistro = false;

    private ObservableCollection<LicenciaViewModel> _licencias;
    private LicenciaViewModel _tempLicenciaViewModel;
    private LicenciaViewModel _licenciaViewModel;

    #region Request
    private RegistrarLicenciaDocenteRequest _registrarLicenciaRequest;
    private EditarLicenciaRequest _editarLicenciaRequest;
    private EditarEstadoLicenciaRequest _editarEstadoLicenciaRequest;
    #endregion

    #region Response
    private LicenciaResponse _tempLicenciaResponse;
    #endregion

    #region Commands
    public ViewModelCommand CancelarCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand EditarCommand { get; }
    public ViewModelCommand RegistrarCommand { get; }
    #endregion


    public GestionLicenciasViewModel(IServicioDocentes servicioDocentes, PerfilBuscadoStore perfilBuscadoStore)
    {
        _servicioDocentes = servicioDocentes;
        _perfilBuscadoStore = perfilBuscadoStore;

        ActualizarLicencias();

        LicenciaViewModel = new LicenciaViewModel(null);
        HabilitarLicenciaViewModel = false;
        HabilitarEdicion = false;
        HabilitarRegistro = true;

        CancelarCommand = new ViewModelCommand(ExecuteCancelarCommand, CanExecuteCancelarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        EditarCommand = new ViewModelCommand(ExecuteEditarCommand, CanExecuteEditarCommand);
        RegistrarCommand = new ViewModelCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
    }

    #region Properties
    private void ActualizarLicencias()
    {
        var licencias = _servicioDocentes.BuscarLicencias(_perfilBuscadoStore.PersonaID);
        Licencias = new ObservableCollection<LicenciaViewModel>(licencias.Select(x => new LicenciaViewModel(x)));
    }

    public bool HabilitarEdicion
    {
        get
        {
            return _habilitarEdicion;
        }

        set
        {
            _habilitarEdicion = value;
            OnPropertyChanged(nameof(HabilitarEdicion));
        }
    }

    public bool HabilitarRegistro
    {
        get
        {
            return _habilitarRegistro;
        }

        set
        {
            _habilitarRegistro = value;
            OnPropertyChanged(nameof(HabilitarRegistro));
        }
    }

    public bool HabilitarLicenciaViewModel
    {
        get
        {
            return _habilitarLicenciaViewModel;
        }

        set
        {
            _habilitarLicenciaViewModel = value;
            OnPropertyChanged(nameof(HabilitarLicenciaViewModel));
        }
    }

    public LicenciaViewModel LicenciaViewModel
    {
        get
        {
            return _licenciaViewModel;
        }

        set
        {
            _licenciaViewModel = value;
            OnPropertyChanged(nameof(LicenciaViewModel));
        }
    }

    public ObservableCollection<LicenciaViewModel> Licencias
    {
        get
        {
            return _licencias;
        }

        set
        {
            _licencias = value;
            OnPropertyChanged(nameof(Licencias));
        }
    }
    #endregion

    #region CancelarCommand
    private bool CanExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Editar":
                return HabilitarEdicion && HabilitarLicenciaViewModel && LicenciaViewModel.HayDatos;
            case "Nueva":
                return !HabilitarRegistro && HabilitarLicenciaViewModel;
            default: return false;
        }
    }

    private void ExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Editar":
                HabilitarEdicion = false;
                HabilitarLicenciaViewModel = false;
                ActualizarLicencias();

                break;
            case "Nueva":
                HabilitarRegistro = true;
                HabilitarLicenciaViewModel = false;
                ActualizarLicencias();

                break;
        }
    }
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj)
    {
        switch (obj)
        {
            case "Aprobar":
                return LicenciaViewModel is not null && ((Estado) LicenciaViewModel.EstadoIndex).Equals(Estado.Pendiente) && LicenciaViewModel.HayDatos;
            case "Cancelar":
                return LicenciaViewModel is not null && ((Estado) LicenciaViewModel.EstadoIndex).Equals(Estado.Pendiente) && LicenciaViewModel.HayDatos;
            case "Editar":
                return HabilitarEdicion && HabilitarLicenciaViewModel && !LicenciaViewModel.HasErrors && LicenciaViewModel.HayDatos;
            case "Nueva":
                return !HabilitarRegistro && HabilitarLicenciaViewModel && !LicenciaViewModel.HasErrors;
            default: return false;
        }
    }

    private void ExecuteGuardarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Aprobar":
                messageBoxText = $"¿Está seguro que desea aprobar la licencia { LicenciaViewModel.ArticuloDescripcion } para el docente?";
                caption = "Conceder Licencia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _editarEstadoLicenciaRequest = new EditarEstadoLicenciaRequest(_perfilBuscadoStore.PersonaID,
                                                                                       LicenciaViewModel.ArticuloIndex,
                                                                                       int.Parse(LicenciaViewModel.Dias),
                                                                                       LicenciaViewModel.FechaInicio,
                                                                                       LicenciaViewModel.Observacion);
                        _servicioDocentes.AprobarLicencia(_editarEstadoLicenciaRequest);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        ActualizarLicencias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                break;

            case "Cancelar":
                messageBoxText = $"¿Está seguro que desea rechazar la licencia { LicenciaViewModel.ArticuloDescripcion } para el docente?";
                caption = "Rechazar Licencia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _editarEstadoLicenciaRequest = new EditarEstadoLicenciaRequest(_perfilBuscadoStore.PersonaID,
                                                                                       LicenciaViewModel.ArticuloIndex,
                                                                                       int.Parse(LicenciaViewModel.Dias),
                                                                                       LicenciaViewModel.FechaInicio,
                                                                                       LicenciaViewModel.Observacion);
                        _servicioDocentes.CancelarLicencia(_editarEstadoLicenciaRequest);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        ActualizarLicencias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                break;

            case "Editar":
                messageBoxText = $"¿Está seguro que desea modificar la licencia del docente?";
                caption = "Editar Licencia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _editarLicenciaRequest = new EditarLicenciaRequest(_perfilBuscadoStore.PersonaID,
                                                                           _tempLicenciaResponse,
                                                                           LicenciaViewModel.ArticuloIndex,
                                                                           int.Parse(LicenciaViewModel.Dias),
                                                                           LicenciaViewModel.FechaInicio,
                                                                           LicenciaViewModel.Observacion);
                        _tempLicenciaResponse = _servicioDocentes.ModificarLicencia(_editarLicenciaRequest);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        _licencias.Remove(_tempLicenciaViewModel);
                        _licencias.Add(new LicenciaViewModel(_tempLicenciaResponse));

                        //ActualizarLicencias();
                        HabilitarEdicion = false;
                        HabilitarLicenciaViewModel = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                HabilitarLicenciaViewModel = false;
                HabilitarEdicion = false;

                break;

            case "Nueva":
                messageBoxText = $"¿Está seguro que desea cargar una licencia ({ LicenciaViewModel.ArticuloDescripcion }) para el docente?";
                caption = "Registrar Licencia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _registrarLicenciaRequest = new RegistrarLicenciaDocenteRequest(_perfilBuscadoStore.PersonaID,
                                                                                        LicenciaViewModel.ArticuloIndex,
                                                                                        int.Parse(LicenciaViewModel.Dias),
                                                                                        LicenciaViewModel.FechaInicio,
                                                                                        LicenciaViewModel.Observacion);
                        var licenciaRegistrada = _servicioDocentes.RegistrarLicencia(_registrarLicenciaRequest);
                        _licencias.Add(new LicenciaViewModel(licenciaRegistrada));

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                HabilitarLicenciaViewModel = false;
                HabilitarRegistro = true;

                break;
        }
    }
    #endregion

    #region EditarCommand
    private bool CanExecuteEditarCommand(object obj)
    {
        switch (obj)
        {
            case "Editar":
                return !HabilitarEdicion && !HabilitarLicenciaViewModel && LicenciaViewModel is not null && ((Estado)LicenciaViewModel.EstadoIndex).Equals(Estado.Pendiente) && LicenciaViewModel.HayDatos;
            default:
                return false;
        }
    }

    private void ExecuteEditarCommand(object obj)
    {
        switch (obj)
        {
            case "Editar":
                _tempLicenciaViewModel = LicenciaViewModel;
                _tempLicenciaResponse = new LicenciaResponse(LicenciaViewModel.ArticuloIndex,
                                                             LicenciaViewModel.EstadoIndex,
                                                             int.Parse(LicenciaViewModel.Dias),
                                                             LicenciaViewModel.FechaInicio,
                                                             LicenciaViewModel.FechaFin,
                                                             LicenciaViewModel.Observacion);
                HabilitarEdicion = true;
                HabilitarLicenciaViewModel = true;
                break;
        }
    }
    #endregion

    #region RegistrarCommand
    private bool CanExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Nueva":
                return HabilitarRegistro && !HabilitarEdicion && !HabilitarLicenciaViewModel;
            default:
                return false;
        }
    }

    private void ExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Nueva":
                LicenciaViewModel = new LicenciaViewModel(null);
                HabilitarLicenciaViewModel = true;
                HabilitarRegistro = false;
                break;
        }
    }
    #endregion
}
