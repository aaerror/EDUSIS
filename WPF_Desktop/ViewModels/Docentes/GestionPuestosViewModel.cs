using Core.ServicioCursos.DTOs.Requests;
using Core.ServicioCursos.DTOs.Responses;
using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Domain.Docentes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Shared;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Docentes;

public class GestionPuestosViewModel : ViewModel
{
    private readonly IServicioDocentes _servicioDocentes;
    private PerfilBuscadoStore _perfilBuscadoStore;

    private bool _habilitarEditarPuesto = false;
    private bool _habilitarNuevoPuesto = false;
    private bool _habilitarPuestoInfo = false;

    private string _legajo = string.Empty;
    private string _documento = string.Empty;
    private string _nombreCompleto = string.Empty;
    private PuestoDocenteViewModel _puestoDocenteViewModel;

    #region Response
    private DocenteConPuestosResponse _docenteConPuestosResponse;
    private PuestoResponse _puestoDocenteResponse;
    #endregion

    #region Request
    private CrearPuestoDocenteRequest _crearPuestoDocenteRequest;
    private EliminarPuestoDocenteRequest _eliminarPuestoDocente;
    #endregion

    private IReadOnlyCollection<PuestoDocenteViewModel> _tempPuestos = new List<PuestoDocenteViewModel>();
    private ObservableCollection<PuestoDocenteViewModel> _puestos;

    #region Commands
    public ViewModelCommand CancelarCommand { get; }
    public ViewModelCommand EditarCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand RegistrarCommand { get; }
    #endregion


    public GestionPuestosViewModel(IServicioDocentes servicioDocentes, PerfilBuscadoStore perfilBuscadoStore)
    {
        _servicioDocentes = servicioDocentes;
        _perfilBuscadoStore = perfilBuscadoStore;

        _docenteConPuestosResponse = _servicioDocentes.BuscarDocenteConPuestos(perfilBuscadoStore.PersonaID);
        ActualizarPuestos();

        /*_tempPuestos = _docenteConPuestosResponse.Puestos.Select(x => new PuestoDocenteViewModel(x)).ToList();
        Puestos = new ObservableCollection<PuestoDocenteViewModel>(_tempPuestos);*/

        HabilitarEditarPuesto = false;
        HabilitarNuevoPuesto = false;
        HabilitarPuestoInfo = false;

        Legajo = _docenteConPuestosResponse.Legajo;
        Documento = _docenteConPuestosResponse.Documento;
        NombreCompleto = _docenteConPuestosResponse.NombreCompleto;

        CancelarCommand = new ViewModelCommand(ExecuteCancelarCommand, CanExecuteCancelarCommand);
        EditarCommand = new ViewModelCommand(ExecuteEditarCommand, CanExecuteEditarCommand);
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        RegistrarCommand = new ViewModelCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
    }

    private void ActualizarPuestos()
    {
        var puestos = _servicioDocentes.BuscarPuestos(_perfilBuscadoStore.PersonaID);
        _puestos = new ObservableCollection<PuestoDocenteViewModel>(puestos.Select(x => new PuestoDocenteViewModel(x)));
    }

    #region Properties
    public bool HabilitarEditarPuesto
    {
        get
        {
            return _habilitarEditarPuesto;
        }

        set
        {
            _habilitarEditarPuesto = value;
            OnPropertyChanged(nameof(HabilitarEditarPuesto));
        }
    }

    public bool HabilitarNuevoPuesto
    {
        get
        {
            return _habilitarNuevoPuesto;
        }

        set
        {
            _habilitarNuevoPuesto = value;
            OnPropertyChanged(nameof(HabilitarNuevoPuesto));
        }
    }

    public bool HabilitarPuestoInfo
    {
        get
        {
            return _habilitarPuestoInfo;
        }

        set
        {
            _habilitarPuestoInfo = value;
            OnPropertyChanged(nameof(HabilitarPuestoInfo));
        }
    }

    public string Legajo
    {
        get
        {
            return _legajo;
        }

        set
        {
            _legajo = value;
            OnPropertyChanged(nameof(Legajo));
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

    public string NombreCompleto
    {
        get
        {
            return _nombreCompleto;
        }

        set
        {
            _nombreCompleto = value;
            OnPropertyChanged(nameof(NombreCompleto));
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

    public ObservableCollection<PuestoDocenteViewModel> Puestos
    {
        get
        {
            return _puestos;
        }

        /*set
        {
            _puestos = value;
            OnPropertyChanged(nameof(Puestos));
        }*/
    }
    #endregion

    #region CancelarCommand
    private bool CanExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Editar":
                return HabilitarEditarPuesto && !HabilitarNuevoPuesto;
            case "Nuevo":
                return HabilitarNuevoPuesto && !HabilitarEditarPuesto;
            default: return false;
        }
    }

    private void ExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Editar":
                _puestos = new ObservableCollection<PuestoDocenteViewModel>(_tempPuestos);
                HabilitarEditarPuesto = false;
                HabilitarPuestoInfo = false;
                break;
            case "Nuevo":
                _puestos = new ObservableCollection<PuestoDocenteViewModel>(_tempPuestos);
                HabilitarNuevoPuesto = false;
                HabilitarPuestoInfo = false;
                break;
        }
    }
    #endregion

    #region EditarCommand
    private bool CanExecuteEditarCommand(object obj)
    {
        return PuestoDocenteViewModel is not null && !HabilitarEditarPuesto && !HabilitarNuevoPuesto;
    }
    private void ExecuteEditarCommand(object obj)
    {
        HabilitarPuestoInfo = true;
        HabilitarEditarPuesto = true;
    }
    #endregion

    #region EliminarCommand
    private bool CanExecuteEliminarCommand(object obj)
    {
        return PuestoDocenteViewModel is not null && PuestoDocenteViewModel.FechaFin is null && !HabilitarEditarPuesto && !HabilitarNuevoPuesto;
    }

    private void ExecuteEliminarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;
        switch (obj)
        {
            case "PuestoDocente":
                messageBoxText = $"¿Está seguro que desea dar de baja a { NombreCompleto } del puesto { PuestoDocenteViewModel.PosicionDescripcion }?";
                caption = "Quitar Puesto";
                
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var puestoIndex = _puestos.IndexOf(PuestoDocenteViewModel);
                        _eliminarPuestoDocente = new EliminarPuestoDocenteRequest(_docenteConPuestosResponse.DocenteID,
                                                                                  PuestoDocenteViewModel.PosicionIndex,
                                                                                  PuestoDocenteViewModel.FechaInicio);

                        _puestoDocenteResponse = _servicioDocentes.QuitarPuestoDocente(_eliminarPuestoDocente);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                ActualizarPuestos();
                break;
        }
    }
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj)
    {
        switch (obj)
        {
            case "Editar":
                return HabilitarEditarPuesto && !HabilitarNuevoPuesto;
            case "Nuevo":
                return HabilitarNuevoPuesto && !HabilitarEditarPuesto;
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
            case "Editar":
                messageBoxText = $"¿Está seguro que desea modificar el puesto del docente { NombreCompleto }?\n" +
                                 $"Puesto Nuevo:" +
                                 $"\tCargo: { (Posicion) PuestoDocenteViewModel.PosicionIndex }\n" +
                                 $"\tFecha Inicio: { PuestoDocenteViewModel.FechaInicio.Date.ToString("D") }\n\n";
                caption = "Modificar Puesto Docente";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _crearPuestoDocenteRequest = new CrearPuestoDocenteRequest(_docenteConPuestosResponse.DocenteID,
                                                                                   PuestoDocenteViewModel.PosicionIndex,
                                                                                   PuestoDocenteViewModel.FechaInicio);
                        _puestoDocenteResponse = _servicioDocentes.AsignarPuestoDocente(_crearPuestoDocenteRequest);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        ActualizarPuestos();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    ActualizarPuestos();
                }

                HabilitarEditarPuesto = false;
                HabilitarPuestoInfo = false;

                break;
            case "Nuevo":
                messageBoxText = $"¿Está seguro que desea agregar el docente { NombreCompleto } al puesto { (Posicion) PuestoDocenteViewModel.PosicionIndex }?";
                caption = "Asignar un Nuevo Puesto";

                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        _crearPuestoDocenteRequest = new CrearPuestoDocenteRequest(_docenteConPuestosResponse.DocenteID,
                                                                                   PuestoDocenteViewModel.PosicionIndex,
                                                                                   PuestoDocenteViewModel.FechaInicio);
                        _puestoDocenteResponse = _servicioDocentes.AsignarPuestoDocente(_crearPuestoDocenteRequest);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        PuestoDocenteViewModel = new PuestoDocenteViewModel(_puestoDocenteResponse);
                        Puestos.Add(PuestoDocenteViewModel);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    ActualizarPuestos();
                }

                HabilitarNuevoPuesto = false;
                HabilitarPuestoInfo = false;

                break;
        }
    }
    #endregion

    #region RegistrarCommand
    private bool CanExecuteRegistrarCommand(object obj)
    {
        return !HabilitarEditarPuesto && !HabilitarNuevoPuesto;
    }

    private void ExecuteRegistrarCommand(object obj)
    {
        HabilitarPuestoInfo = true;
        HabilitarNuevoPuesto = true;
        PuestoDocenteViewModel = new(null);
    }
    #endregion
}
