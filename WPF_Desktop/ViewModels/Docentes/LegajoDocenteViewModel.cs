using Core.ServicioDocentes.DTOs.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using WPF_Desktop.Shared;

namespace WPF_Desktop.ViewModels.Docentes;

public class LegajoDocenteViewModel : ViewModel, INotifyDataErrorInfo
{
    private readonly LegajoDocenteResponse _legajoDocenteResponse = null;

    private Guid _docenteID = Guid.Empty;
    private string _nombreCompleto = string.Empty;
    private string _legajo = string.Empty;
    private string _cuil = string.Empty;
    private string _prefijoCuil = string.Empty;
    private string _dni = string.Empty;
    private string _posfijoCuil = string.Empty;
    private DateTime _fechaAlta;
    private DateTime? _fechaBaja = null;
    private bool _estaActivo = false;

    private bool _esRegistroDocente = false;

    private Dictionary<string, List<string>> _errorsByProperty = new();
    public bool HasErrors => _errorsByProperty.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;


    public LegajoDocenteViewModel(LegajoDocenteResponse legajoDocenteResponse)
    {
        if (legajoDocenteResponse is not null)
        {
            _legajoDocenteResponse = legajoDocenteResponse;
            string cuil = _legajoDocenteResponse.CUIL;

            DocenteID = _legajoDocenteResponse.DocenteID;
            NombreCompleto = _legajoDocenteResponse.NombreCompleto;
            Legajo = _legajoDocenteResponse.Legajo;
            PrefijoCuil = cuil.Remove(2);
            DNI = _legajoDocenteResponse.CUIL.Substring(2,8);
            PosfijoCuil = cuil[cuil.Length - 1].ToString();
            FechaAlta = _legajoDocenteResponse.FechaAlta;
            FechaBaja = _legajoDocenteResponse.FechaBaja;
            EstaActivo = _legajoDocenteResponse.EstaActivo;
        }

        FechaAlta = DateTime.Now;
        EstaActivo = true;
    }

    #region Properties
    public Guid DocenteID
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

    public string Legajo
    {
        get
        {
            return _legajo;
        }

        set
        {
            _errorsByProperty.Remove(nameof(Legajo));
            _legajo = value;
            OnPropertyChanged(nameof(Legajo));

            if (string.IsNullOrWhiteSpace(Legajo))
            {
                _errorsByProperty.Add(nameof(Legajo), new List<string>
                {
                    "Se debe ingresar un legajo docente."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Legajo)));
            }
            else
            {
                if (!Regex.IsMatch(Legajo, @"^(\d{6})$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(Legajo), new List<string>
                    {
                        "El legajo posee un formato inválido. Se debe ingresar un número de seis dígitos."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Legajo)));
                }
/*                else
                {
                    if (_servicioDocentes.EsLegajoInvalido(Legajo))
                    {
                        _errorsByProperty.Add(nameof(Legajo), new List<string>
                        {
                            "El legajo especificado ya se encuentra registrado por otro docente."
                        });

                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Legajo)));
                    }
                }*/
            }
        }
    }

    public string CUIL
    {
        get
        {
            return PrefijoCuil + DNI + PosfijoCuil;
        }
    }

    public string PrefijoCuil
    {
        get
        {
            return _prefijoCuil;
        }

        set
        {
            _errorsByProperty.Remove(nameof(PrefijoCuil));
            _prefijoCuil = value;
            OnPropertyChanged(nameof(PrefijoCuil));

            if (string.IsNullOrWhiteSpace(PrefijoCuil))
            {
                _errorsByProperty.Add(nameof(PrefijoCuil), new List<string>
                {
                    "Se debe ingresar el prefijo del CUIL."
                });

                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PrefijoCuil)));
            }
            else
            {
                if (!Regex.IsMatch(PrefijoCuil, @"^(2[0347])$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(PrefijoCuil), new List<string>
                    {
                        "El prefijo del CUIL es incorrecto. Los prefijos habilitados son: 20 - 23 - 24 - 27"
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PrefijoCuil)));
                }/*
                else
                {
                    string cuil = PrefijoCuil + InformacionPersonalViewModel.Documento + PosfijoCuil;
                    if (_servicioDocentes.EsCuilInvalido(cuil))
                    {
                        _errorsByProperty.Add(nameof(PrefijoCuil), new List<string>
                        {
                            "El CUIL ya se encuentra registrado por otro docente."
                        });

                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PrefijoCuil)));
                    }
                }*/
            }
        }
    }

    public string DNI
    {
        get
        {
            return _dni;
        }

        set
        {
            _dni = value;
            OnPropertyChanged(nameof(DNI));
        }
    }

    public string PosfijoCuil
    {
        get
        {
            return _posfijoCuil;
        }

        set
        {
            _errorsByProperty.Remove(nameof(PosfijoCuil));
            _posfijoCuil = value;
            OnPropertyChanged(nameof(PosfijoCuil));

            if (string.IsNullOrWhiteSpace(PosfijoCuil))
            {
                _errorsByProperty.Add(nameof(PosfijoCuil), new List<string>
                {
                    "Se debe ingresar el posfijo del CUIL."
                });
            }
            else
            {
                if (!Regex.IsMatch(PosfijoCuil, @"^\d{1}$", RegexOptions.None, TimeSpan.FromMilliseconds(2500)))
                {
                    _errorsByProperty.Add(nameof(PosfijoCuil), new List<string>
                    {
                        "El posfijo del CUIL debe ser un número."
                    });

                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PosfijoCuil)));
                }
/*                else
                {
                    string cuil = PrefijoCuil + InformacionPersonalViewModel.Documento + PosfijoCuil;
                    if (_servicioDocentes.EsCuilInvalido(cuil))
                    {
                        _errorsByProperty.Add(nameof(PosfijoCuil), new List<string>
                        {
                            "El CUIL ya se encuentra registrado por otro docente."
                        });

                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PosfijoCuil)));
                    }
                }*/
            }
        }
    }

    public DateTime FechaAlta
    {
        get
        {
            return _fechaAlta;
        }

        set
        {
            _fechaAlta = value;
            OnPropertyChanged(nameof(FechaAlta));
        }
    }

    public DateTime? FechaBaja
    {
        get
        {
            return _fechaBaja;
        }

        set
        {
            _fechaBaja = value;
            OnPropertyChanged(nameof(FechaBaja));
        }
    }

    public bool EstaActivo
    {
        get
        {
            return _estaActivo;
        }

        set
        {
            _estaActivo = value;
            OnPropertyChanged(nameof(EstaActivo));
        }
    }
    #endregion

    #region DataErrors
    public IEnumerable GetErrors(string? propertyName) => _errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
    #endregion
}
