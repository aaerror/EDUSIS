using Core.ServicioDocentes;
using Core.ServicioDocentes.DTOs.Requests;
using Core.ServicioDocentes.DTOs.Responses;
using Domain.Docentes.Licencias;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

public class LicenciaViewModel : ViewModel, INotifyDataErrorInfo
{
    #region Response
    private LicenciaResponse _licenciaResponse;
    #endregion

    private bool _hayDatos = false;

    private int _articuloIndex;
    private string _articuloDescripcion;
    private int _estadoIndex;
    private string _estadoDescripcion;
    private string _dias;
    private DateTime _fechaInicio;
    private DateTime? _fechaFin;
    private string _observacion = string.Empty;


    private Dictionary<string, List<string>> _errorsByProperty = new Dictionary<string, List<string>>();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public LicenciaViewModel(LicenciaResponse licenciaResponse)
    {
        ArticuloIndex = 0;
        EstadoIndex = (int) Estado.Pendiente;
        FechaInicio = DateTime.Today.Date;

        if (licenciaResponse is not null)
        {
            ArticuloIndex = licenciaResponse.Articulo;
            EstadoIndex = licenciaResponse.Estado;
            Dias = licenciaResponse.Dias.ToString();
            FechaInicio = licenciaResponse.FechaInicio;
            FechaFin = licenciaResponse.FechaFin;
            Observacion = licenciaResponse.Observacion;
            HayDatos = true;
        }
    }

    #region Properties
    public bool HayDatos
    {
        get
        {
            return _hayDatos;
        }

        private set
        {
            _hayDatos = value;
            OnPropertyChanged(nameof(HayDatos));
        }
    }

    public int ArticuloIndex
    {
        get
        {
            return _articuloIndex;
        }

        set
        {
            _articuloIndex = value;
            ArticuloDescripcion = Enum.Parse<Articulo>(ArticuloIndex.ToString()).ToString();
            OnPropertyChanged(nameof(ArticuloIndex));
        }
    }

    public string ArticuloDescripcion
    {
        get
        {
            return _articuloDescripcion;
        }

        private set
        {
            _articuloDescripcion = value;
            //OnPropertyChanged(ArticuloDescripcion);
        }
    }

    public int EstadoIndex
    {
        get
        {
            return _estadoIndex;
        }

        set
        {
            _estadoIndex = value;
            EstadoDescripcion = Enum.Parse<Estado>(EstadoIndex.ToString()).ToString();
            OnPropertyChanged(nameof(EstadoIndex));
        }
    }

    public string EstadoDescripcion
    {
        get
        {
            return _estadoDescripcion;
        }

        private set
        {
            _estadoDescripcion = value;
            //OnPropertyChanged(nameof(EstadoDescripcion));
        }
    }

    public string Dias
    {
        get
        {
            return _dias;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Dias));
            _dias = value;
            OnPropertyChanged(nameof(Dias));

            if (string.IsNullOrWhiteSpace(Dias))
            {
                _errorsByProperty.Add(nameof(Dias), new List<string>
                {
                    "Se debe ingresar la cantidad de días de licencia."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Dias)));
            }
            else
            {
                if (!Regex.IsMatch(Dias, @"^(\d){1,3}$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Dias), new List<string>
                    {
                        "Formato inválido. La cantidad de días debe ser un número."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Dias)));
                }
                else
                {
                    var cantidadDias = int.Parse(Dias);
                    if (cantidadDias <= 0 || cantidadDias > 365)
                    {
                        _errorsByProperty.Add(nameof(Dias), new List<string>
                        {
                            "La cantidad de días de licencia debe estar comprendida entre un día hasta un año."
                        });

                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Dias)));
                    }
                }
            }
        }
    }

    public DateTime FechaInicio
    {
        get
        {
            return _fechaInicio;
        }

        set
        {
            _fechaInicio = value.Date;
            OnPropertyChanged(nameof(FechaInicio));

            if (FechaInicio.Date > DateTime.Today.Date)
            {
                _errorsByProperty.Add(nameof(FechaInicio), new List<string>
                {
                    "La fecha de inicio de la licencia no ser superior a la fecha actual."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FechaInicio)));
            }
        }
    }

    public DateTime? FechaFin
    {
        get
        {
            return _fechaFin;
        }

        set
        {
            var cantidadDias = int.Parse(Dias);
            _fechaFin = FechaInicio.AddDays(cantidadDias).Date;
            OnPropertyChanged(nameof(FechaFin));
        }
    }

    public string Observacion
    {
        get
        {
            return _observacion;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Observacion));
            _observacion = value;
            OnPropertyChanged(nameof(Observacion));

            if (!string.IsNullOrWhiteSpace(Observacion))
            {
                if (Observacion.Length > 120)
                {
                    _errorsByProperty.Add(nameof(Observacion), new List<string>
                    {
                        "El detalle de observación no puede superar los 120 caracteres."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Observacion)));
                }
            }
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion
}
