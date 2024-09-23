using Core.ServicioMaterias.DTOs.Requests;
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
using WPF_Desktop.Navigation;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

public class MateriaViewModel : ViewModel, INotifyDataErrorInfo
{
    #region Service
    private readonly IServicioMateria _servicioMateria;
    private readonly INavigationService _gestionSituacionRevistaNavigationService;
    #endregion

    #region Store
    private MateriaStore _materiaStore;
    #endregion

    #region Response
    private readonly MateriaResponse _materiaResponse;
    #endregion

    private bool _editarMateria = false;

    private Guid _materiaID = Guid.Empty;
    private string _descripcion = string.Empty;
    private int _horasCatedra;
    private SituacionRevistaViewModel _situacionRevista = null;

    #region Commands
    public ViewModelCommand NavigateCommand { get; }
    public ViewModelCommand GuardarCommand { get; }
    public ViewModelCommand UpsertCommand { get; }
    public ViewModelCommand CancelarCommand { get; }
    #endregion

    private Dictionary<string, List<string>> _errorsByProperty = new();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public MateriaViewModel(INavigationService gestionSituacionRevistaNavigationService, IServicioMateria servicioMateria, MateriaResponse materiaResponse, MateriaStore materiaStore)
    {
        _servicioMateria = servicioMateria;
        _gestionSituacionRevistaNavigationService = gestionSituacionRevistaNavigationService;
        _materiaStore = materiaStore;

        if (materiaResponse is not null)
        {
            _materiaResponse = materiaResponse;

            MateriaID = _materiaResponse.MateriaID;
            Descripcion = _materiaResponse.Descripcion;
            HorasCatedra = _materiaResponse.HorasCatedra;
            SituacionRevista = _materiaResponse.SituacionRevistaResponse is not null ? new SituacionRevistaViewModel(_materiaResponse.SituacionRevistaResponse) : null;
        }

        EditarMateria = false;

        NavigateCommand = new ViewModelCommand(ExecuteNavigateCommand);
        GuardarCommand = new ViewModelCommand(ExecuteGuardarCommand, CanExecuteGuardarCommand);
        UpsertCommand = new ViewModelCommand(ExecuteUpsertCommand, CanExecuteUpsertCommand);
        CancelarCommand = new ViewModelCommand(ExecuteCancelarCommand, CanExecuteCancelarCommand);
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
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName);
    #endregion

    #region NavigateCommand
    private void ExecuteNavigateCommand(object obj)
    {
        switch (obj)
        {
            case "SituacionRevista":
                _materiaStore.Materia = this;
                _gestionSituacionRevistaNavigationService.Navigate();
                break;
        }
    }
    #endregion

    #region GuardarCommand
    private bool CanExecuteGuardarCommand(object obj)
    {
        switch (obj)
        {
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
            /*case "Docente":
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
                        _servicioMateria.EstablecerDocenteDeAula(request);

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
                break;*/

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
                        _servicioMateria.ModificarMateria(request);

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

    #region UpsertCommand
    private bool CanExecuteUpsertCommand(object obj)
    {
        switch (obj)
        {
            case "Insert":
                return false;
            case "Update":
                return !EditarMateria;
            case "Materia":
                return !EditarMateria;
            default:
                return false;
        }
    }

    private void ExecuteUpsertCommand(object obj)
    {
        switch (obj)
        {
            case "Insert":
                break;
            case "Update":
                break;
            case "Materia":
                EditarMateria = true;
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
            case "Materia":
                EditarMateria = false;
                Descripcion = _materiaResponse.Descripcion;
                MateriaID = _materiaResponse.MateriaID;
                Descripcion = _materiaResponse.Descripcion;
                HorasCatedra = _materiaResponse.HorasCatedra;
                SituacionRevista = new SituacionRevistaViewModel(_materiaResponse.SituacionRevistaResponse);

                break;
        }
        //Profesores.Concat(_materiaResponse.Profesores);
        //Horarios.Concat(_materiaResponse.Horarios);
    }
    #endregion
}
