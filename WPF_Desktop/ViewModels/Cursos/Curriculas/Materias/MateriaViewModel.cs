﻿using Core.ServicioMaterias.DTOs.Requests;
using Core.ServicioMaterias.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPF_Desktop.Shared;
using Core.ServicioMaterias;
using System.Windows.Controls;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

public class MateriaViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly IServicioMateria _servicioMaterias;

    #region Response
    private readonly MateriaResponse _materiaResponse;
    #endregion

    private bool _editarMateria = false;
    private bool _verLista = false;
    private bool _listarHorarios = false;
    private DataGrid _historialDataGrid;

    private Guid _materiaID = Guid.Empty;
    private string _descripcion = string.Empty;
    private int _horasCatedra;
    private Guid? _docenteID = Guid.Empty;
    private string _nombreCompletoProfesor = string.Empty;
    private SituacionRevistaViewModel _situacionRevista = null;

    private ObservableCollection<SituacionRevistaViewModel> _historial = new();
    private ObservableCollection<HorarioViewModel> _horarios = new();

    private Dictionary<string, List<string>> _errorsByProperty = new();

    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #region Commands
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand RegistrarCommand { get; }
    public ViewModelCommand CancelarCommand { get; }
    public ViewModelCommand EliminarCommand { get; }
    public ViewModelCommand ListarCommand { get; }
    #endregion


    public MateriaViewModel(IServicioMateria servicioMaterias, MateriaResponse materiaResponse)
    {
        _servicioMaterias = servicioMaterias;
        _historial.Clear();
        _horarios.Clear();

        if (materiaResponse is not null)
        {
            _materiaResponse = materiaResponse;

            MateriaID = _materiaResponse.MateriaID;
            Descripcion = _materiaResponse.Descripcion;
            HorasCatedra = _materiaResponse.HorasCatedra;
            DocenteID = _materiaResponse.ProfesorID;
            NombreCompletoProfesor = _materiaResponse.NombreCompletoProfesor;
        }

        EditarMateria = false;
        VerLista = false;

        CancelarCommand = new ViewModelCommand(ExecuteCancelarCommand, CanExecuteCancelarCommand);
        RegistrarCommand = new ViewModelCommand(ExecuteRegistrarCommand, CanExecuteRegistrarCommand);
        EliminarCommand = new ViewModelCommand(ExecuteEliminarCommand, CanExecuteEliminarCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        ListarCommand = new ViewModelCommand(ExecuteListarCommand, CanExecuteListarCommand);
    }

    public void AgregarDocenteEnFunciones(Guid docenteID)
    {
        var situacionRevista = _historial.Where(x => x.DocenteID.Equals(docenteID) && x.FechaBaja is null)
                                         .FirstOrDefault();
        situacionRevista.EnFunciones = true;
        DocenteID = situacionRevista.DocenteID;
        NombreCompletoProfesor = situacionRevista.Docente;
    }

    public void QuitarDocenteEnFunciones(SituacionRevistaViewModel situacionRevista)
    {
        if (situacionRevista is not null)
        {
            situacionRevista.EnFunciones = false;

            DocenteID = Guid.Empty;
            NombreCompletoProfesor = string.Empty;
        }
    }

    public DataGrid HistorialDataGrid
    {
        get
        {
            return _historialDataGrid;
        }

        set
        {
            _historialDataGrid = value;
            OnPropertyChanged(nameof(HistorialDataGrid));
        }
    }

    #region Properties
    public bool EditarMateria
    {
        get
        {
            return _editarMateria;
        }

        set
        {
            _editarMateria = value;
            OnPropertyChanged(nameof(EditarMateria));
        }
    }

    public bool VerLista
    {
        get { return _verLista; }
        set
        {
            _verLista = value;
            OnPropertyChanged(nameof(VerLista));
        }
    }

    public bool ListarHorarios
    {
        get
        {
            return _listarHorarios;
        }

        set
        {
            _listarHorarios = value;
            OnPropertyChanged(nameof(ListarHorarios));
        }
    }

    public Guid MateriaID
    {
        get
        {
            return _materiaID;
        }
        private set
        {
            _materiaID = value;
            OnPropertyChanged(nameof(MateriaID));
        }
    }

    public string Descripcion
    {
        get
        {
            return _descripcion;
        }
        set
        {
            _errorsByProperty.Remove(nameof(Descripcion));
            _descripcion = value;
            OnPropertyChanged(nameof(Descripcion));
            

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                _errorsByProperty.Add(nameof(Descripcion), new List<string>
                {
                    "Se debe especificar el nombre de la materia."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Descripcion)));
            }
        }
    }

    public int HorasCatedra
    {
        get
        {
            return _horasCatedra;
        }

        set
        {
            _horasCatedra = value;
            OnPropertyChanged(nameof(HorasCatedra));
        }
    }

    public int HorasCatedraSinAsignar
    {
        get
        {
            return HorasCatedra - _horarios.Count();
        }
    }

    public Guid? DocenteID
    {
        get
        {
            return _docenteID;
        }
        set
        {
            _docenteID = value;
            OnPropertyChanged(nameof(DocenteID));
        }
    }

    public string NombreCompletoProfesor
    {
        get
        {
            return _nombreCompletoProfesor;
        }

        set
        {
            _nombreCompletoProfesor = value;
            OnPropertyChanged(nameof(NombreCompletoProfesor));
        }
    }

    public int CantidadProfesores
    {
        get
        {
            return _historial.Count();
        }
    }

    public SituacionRevistaViewModel SituacionRevista
    {
        get
        {
            return _situacionRevista;
        }

        set
        {
            _situacionRevista = value;
            OnPropertyChanged(nameof(SituacionRevista));
        }
    }

    public ObservableCollection<SituacionRevistaViewModel> Historial
    {
        get
        {
            return _historial;
        }

        set
        {
            _historial = value;
            OnPropertyChanged(nameof(Historial));
        }
    }

    public ObservableCollection<HorarioViewModel> Horarios
    {
        get
        {
            return _horarios;
        }

        set
        {
            _horarios = value;
            OnPropertyChanged(nameof(Horarios));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName);
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                return SituacionRevista is not null;
            case "Materia":
                return EditarMateria;
            default:
                return false;
        }
    }

    private void ExecuteGuardarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Docente":
                messageBoxText = $"¿Está seguro que desea establecer el docente {SituacionRevista.Docente} a cargo de la materia {Descripcion}?";
                caption = "Docente a Cargo de la Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        var situacionRevistaEnFunciones = _historial.Where(x => x.EnFunciones && x.FechaBaja is null)
                                                                    .FirstOrDefault();
                        QuitarDocenteEnFunciones(situacionRevistaEnFunciones);

                        var request = new EstablecerDocenteDeAulaRequest(CursoID: _materiaResponse.CursoID,
                                                                         MateriaID: _materiaResponse.MateriaID,
                                                                         DocenteID: SituacionRevista.DocenteID);
                        _servicioMaterias.EstablecerDocenteDeAula(request);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        AgregarDocenteEnFunciones(SituacionRevista.DocenteID);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                break;

            case "Materia":
                messageBoxText = $"¿Está seguro que desea actualizar los datos de la materia?";
                caption = "Editar Materia";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        var request = new ModificarMateriaRequest(CursoID: _materiaResponse.CursoID,
                                                                  MateriaID: _materiaResponse.MateriaID,
                                                                  Descripcion: Descripcion,
                                                                  HorasCatedra: HorasCatedra);
                        _servicioMaterias.ModificarMateria(request);

                        messageBoxText = $"Cambios guardados exitósamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                break;
        }
    }
    #endregion

    #region RegistrarCommand
    private bool CanExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Materia":
                return !EditarMateria;
            case "Horario":
                return !EditarMateria;
            default:
                return false;
        }
    }

    private void ExecuteRegistrarCommand(object obj)
    {
        switch (obj)
        {
            case "Materia":
                EditarMateria = true;
                break;
            case "Horiario":
                break;
        }
    }
    #endregion

    #region CancelarCommand
    private bool CanExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                return SituacionRevista is not null;
            case "Materia":
                return EditarMateria;
            default:
                return false;
        }
    }

    private void ExecuteCancelarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                HistorialDataGrid.SelectedItem = null;
                break;
            case "Materia":
                EditarMateria = false;
                Descripcion = _materiaResponse.Descripcion;
                MateriaID = _materiaResponse.MateriaID;
                Descripcion = _materiaResponse.Descripcion;
                HorasCatedra = _materiaResponse.HorasCatedra;
                DocenteID = _materiaResponse.ProfesorID;

                break;
        }
        //Profesores.Concat(_materiaResponse.Profesores);
        //Horarios.Concat(_materiaResponse.Horarios);
    }
    #endregion

    #region EliminarCommand
    private bool CanExecuteEliminarCommand(object obj)
    {
        switch (obj)
        {
            case "Docente":
                return SituacionRevista is not null && !SituacionRevista.FechaBaja.HasValue && SituacionRevista.EnFunciones;
            default:
                return false;
        }
    }

    private void ExecuteEliminarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Docente":
                messageBoxText = $"¿Está seguro que desea cambiar de situación de revista al docente {SituacionRevista.Docente} " +
                                 $"de la materia {Descripcion}? Se va a eliminar al docente del cargo {SituacionRevista.CargoDescripcion} " +
                                 $"(Fecha Alta: {SituacionRevista.FechaAlta.ToString("D")})";
                caption = "Cambio de Situación de Revista";
                result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result is MessageBoxResult.Yes)
                {
                    try
                    {
                        var request = new EliminarSituacionRevistaRequest(CursoID: _materiaResponse.CursoID,
                                                                          MateriaID: _materiaResponse.MateriaID,
                                                                          DocenteID: SituacionRevista.DocenteID);
                        _servicioMaterias.QuitarDocenteDeMateria(request);

                        messageBoxText = $"Se quitó del cargo al docente correctamente.";
                        caption = "Operación Exitosa";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                        SituacionRevista.FechaBaja = DateTime.Now.Date;
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error en la operación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                SituacionRevista = null;
                break;

            case "Horario":

                break;
        }
    }
    #endregion

    #region ListarCommand
    private bool CanExecuteListarCommand(object obj) => !EditarMateria;

    private void ExecuteListarCommand(object obj)
    {
        string messageBoxText = string.Empty;
        string caption = string.Empty;
        MessageBoxResult result;

        switch (obj)
        {
            case "Horarios":
                var listarHorariosRequest = new ListarHorariosRequest(CursoID: _materiaResponse.CursoID,
                                                                      MateriaID: _materiaResponse.MateriaID);
                var horarios = _servicioMaterias.ListarHorariosDeMateria(listarHorariosRequest);
                if (horarios.Count is 0)
                {
                    messageBoxText = $"No existen horarios asignados en la materia.";
                    caption = "Horarios Asignados";
                    result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                _horarios.Clear();
                ListarHorarios = true;
                foreach (HorarioResponse horario in horarios)
                {
                    _horarios.Add(new HorarioViewModel(horario));
                }
                break;

            case "SituacionRevista":
                var listarCargosDocentesRequest = new ListarCargosDocentesSegunMateriaRequest(CursoID: _materiaResponse.CursoID,
                                                                                              MateriaID: _materiaResponse.MateriaID);
                var historico = _servicioMaterias.ListarCargosDocenteSegunMateria(listarCargosDocentesRequest);
                if (historico.Count is 0)
                {
                    messageBoxText = $"No existen registros históricos de la situación de revista de los docentes en la materia.";
                    caption = "Historial de Situación de Revista";
                    result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }

                _historial.Clear();
                VerLista = true;
                foreach (SituacionRevistaResponse response in historico)
                {
                    _historial.Add(new SituacionRevistaViewModel(response));
                }

                break;
        }
    }
    #endregion
}
