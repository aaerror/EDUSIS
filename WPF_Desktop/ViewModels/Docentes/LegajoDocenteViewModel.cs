using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioDocentes.DTOs.Responses;
using System.ComponentModel.DataAnnotations;
using System;

namespace WPF_Desktop.ViewModels.Docentes;

internal partial class LegajoDocenteViewModel : ObservableValidator
{
	private readonly LegajoDocenteResponse _legajoDocenteResponse = null;

	[ObservableProperty]
	private Guid _docenteID = Guid.Empty;

	[ObservableProperty]
	private string _nombreCompleto = string.Empty;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar el legajo")]
	[RegularExpression(@"^(\d{6})$", ErrorMessage= "Se debe ingresar un número de seis dígitos.")]
	[Range(6,6)]
	[ObservableProperty]
	private string _legajo = string.Empty;

	public string CodigoUnicoIdentificacionLaboral => $"{ PrefijoCuil }{ DocumentoNacionalIdentidad }{ PosfijoCuil }";

	[NotifyPropertyChangedFor(nameof(CodigoUnicoIdentificacionLaboral))]
	[ObservableProperty]
	private string _prefijoCuil = string.Empty;

	[NotifyPropertyChangedFor(nameof(CodigoUnicoIdentificacionLaboral))]
	[ObservableProperty]
	private string _documentoNacionalIdentidad = string.Empty;

	[NotifyPropertyChangedFor(nameof(CodigoUnicoIdentificacionLaboral))]
	[ObservableProperty]
	private string _posfijoCuil = string.Empty;

	[ObservableProperty]
	private DateTime _fechaAlta = DateTime.Now.Date;

	[ObservableProperty]
	private DateTime? _fechaBaja = null;

	[ObservableProperty]
	private bool _estaActivo = true;

	private bool _esRegistroDocente = false;

	//private Dictionary<string, List<string>> _errorsByProperty = new();
	//public bool HasErrors => _errorsByProperty.Any();


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
			DocumentoNacionalIdentidad = _legajoDocenteResponse.DNI;
			//DNI = CUIL.Substring(2,8);
			PosfijoCuil = cuil[cuil.Length - 1].ToString();
			FechaAlta = _legajoDocenteResponse.FechaInicio;
			FechaBaja = _legajoDocenteResponse.FechaFin;
			EstaActivo = _legajoDocenteResponse.Activo;
		}
	}

	#region Properties
	/*
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

				//else
				//{
				//	if (_servicioDocentes.EsLegajoInvalido(Legajo))
				//	{
				//		_errorsByProperty.Add(nameof(Legajo), new List<string>
				//		{
				//			"El legajo especificado ya se encuentra registrado por otro docente."
				//		});

				//		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Legajo)));
				//	}
				//}
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
				}
				//else
				//{
				//	string cuil = PrefijoCuil + InformacionPersonalViewModel.Documento + PosfijoCuil;
				//	if (_servicioDocentes.EsCuilInvalido(cuil))
				//	{
				//		_errorsByProperty.Add(nameof(PrefijoCuil), new List<string>
				//		{
				//			"El CUIL ya se encuentra registrado por otro docente."
				//		});

				//		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PrefijoCuil)));
				//	}
				//}
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
				//else
				//{
				//	string cuil = PrefijoCuil + InformacionPersonalViewModel.Documento + PosfijoCuil;
				//	if (_servicioDocentes.EsCuilInvalido(cuil))
				//	{
				//		_errorsByProperty.Add(nameof(PosfijoCuil), new List<string>
				//		{
				//			"El CUIL ya se encuentra registrado por otro docente."
				//		});

				//		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PosfijoCuil)));
				//	}
				//}
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
	*/
	#endregion

	/*
	#region DataErrors
	public IEnumerable GetErrors(string? propertyName) =>
		_errorsByProperty.GetValueOrDefault(propertyName).AsEnumerable();
	#endregion
	*/
}