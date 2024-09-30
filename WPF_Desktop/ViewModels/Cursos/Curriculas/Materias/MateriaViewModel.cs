using Core.ServicioMaterias.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WPF_Desktop.Shared;
using Core.ServicioMaterias;
using WPF_Desktop.ViewModels.Cursos.Curriculas.Materias.SituacionRevista;
using WPF_Desktop.Store;

namespace WPF_Desktop.ViewModels.Cursos.Curriculas.Materias;

public class MateriaViewModel : ViewModel, INotifyDataErrorInfo
{
    #region Service
    private readonly IServicioMateria _servicioMateria;
    #endregion

    #region Response
    private readonly MateriaResponse _materiaResponse;
    #endregion

    private Guid _materiaID = Guid.Empty;
    private string _descripcion = string.Empty;
    private int _horasCatedra;
    private int _cargosOcupados;
    private SituacionRevistaViewModel _situacionRevista = null;


    private Dictionary<string, List<string>> _errorsByProperty = new();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public MateriaViewModel(IServicioMateria servicioMateria, MateriaResponse materiaResponse, MateriaStore materiaStore)
    {
        _servicioMateria = servicioMateria;

        if (materiaResponse is not null)
        {
            _materiaResponse = materiaResponse;

            MateriaID = _materiaResponse.MateriaID;
            Descripcion = _materiaResponse.Descripcion;
            HorasCatedra = _materiaResponse.HorasCatedra;
            CargosOcupados = _materiaResponse.CargosOcupados;
            SituacionRevista = _materiaResponse.SituacionRevistaResponse is not null ? new SituacionRevistaViewModel(_materiaResponse.SituacionRevistaResponse) : null;
        }
    }

    #region Properties
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

    public int CargosOcupados
    {
        get
        {
            return _cargosOcupados;
        }

        set
        {
            _cargosOcupados = value;
            OnPropertyChanged(nameof(CargosOcupados));
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
}
