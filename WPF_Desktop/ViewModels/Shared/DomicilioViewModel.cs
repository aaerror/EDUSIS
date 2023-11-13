using Core.Shared.DTOs.Personas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Shared;

public class DomicilioViewModel : ViewModel, INotifyDataErrorInfo
{
    private string _calle = string.Empty;
    private string _altura = string.Empty;
    private int _vivienda = 0;
    private string _observaciones = string.Empty;
    private string _localidad = string.Empty;
    private string _provincia = string.Empty;
    private string _pais = "Argentina";

    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public DomicilioViewModel(DomicilioDTO domicilioDTO)
    {
        if (domicilioDTO is not null)
        {
            Calle = domicilioDTO.Calle;
            Altura = domicilioDTO.Altura;
            Vivienda = domicilioDTO.Vivienda;
            Localidad = domicilioDTO.Localidad;
            Provincia = domicilioDTO.Provincia;
            Pais = domicilioDTO.Pais;
            Observaciones = domicilioDTO.Observacion;
        }
    }

    #region Properties
    public string Calle
    {
        get
        {
            return _calle;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Calle));
            _calle = value;
            OnPropertyChanged(nameof(Calle));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Calle), new List<string>()
                {
                    "Se debe ingresar la calle."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Calle)));
            }
        }
    }

    public string Altura
    {
        get
        {
            return _altura;
        }

        set
        {
            _altura = value;
            OnPropertyChanged(nameof(Altura));
        }
    }

    public int Vivienda
    {
        get
        {
            return _vivienda;
        }

        set
        {
            _vivienda = value;
            OnPropertyChanged(nameof(Vivienda));
        }
    }

    public string Localidad
    {
        get
        {
            return _localidad;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Localidad));
            _localidad = value;
            OnPropertyChanged(nameof(Localidad));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Localidad), new List<string>()
                {
                    "Se debe ingresar la localidad."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Localidad)));
            }
        }
    }

    public string Provincia
    {
        get
        {
            return _provincia;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Provincia));
            _provincia = value;
            OnPropertyChanged(nameof(Provincia));

            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Provincia), new List<string>()
                {
                    "Se debe ingresar la provincia."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Provincia)));
            }
        }
    }

    public string Pais
    {
        get
        {
            return _pais;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Pais));
            _pais = value;
            OnPropertyChanged(nameof(Pais));


            if (string.IsNullOrWhiteSpace(value))
            {
                _errorsByProperty.Add(nameof(Pais), new List<string>()
                {
                    "Se debe ingresar el país."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Pais)));
            }
        }
    }

    public string Observaciones
    {
        get
        {
            return _observaciones;
        }

        set
        {
            _observaciones = value;
            OnPropertyChanged(nameof(Observaciones));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion
}
