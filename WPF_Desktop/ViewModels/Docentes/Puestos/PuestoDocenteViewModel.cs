using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioDocentes.DTOs.Responses;
using Domain.Docentes.Puestos;
using System.ComponentModel.DataAnnotations;
using System;
using WPF_Desktop.Validations;

namespace WPF_Desktop.ViewModels.Docentes.Puestos;

internal partial class PuestoDocenteViewModel : ObservableValidator
{
	private readonly PuestoResponse _puestoDocenteResponse;

	private enum UpsertType
	{
		Insert=1,
		Update=0
	}

	private readonly string _upsert = string.Empty;

	[ObservableProperty]
	private Guid _puestoID = Guid.Empty;

	[Required]
	[EnumDataType(typeof(EstadoPuesto))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _estado = string.Empty;

	[Required]
	[EnumDataType(typeof(Posicion))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _posicion = string.Empty;

	[Required(ErrorMessage="Se debe ingresar la fecha de inicio del puesto docente.")]
	[DataType(DataType.Date, ErrorMessage="Formato inválido. Se debe ingresar una fecha en el siguiente formato dd/mm/aaaa.")]
	[DateAfterThanToday]
	private DateTime _fechaInicio;

	[DataType(DataType.Date, ErrorMessage="Formato inválido. Se debe ingresar una fecha en el siguiente formato dd/mm/aaaa.")]
	[DateAfterOrEqual(nameof(FechaInicio), ErrorMessage="La fecha de finalización debe ser posterior o igual a la fecha de inicio.")]
	private DateTime? _fechaFin;

	private bool _esEventual;


	public PuestoDocenteViewModel()
	{
		_upsert = UpsertType.Insert.ToString();

		Estado = "Pendiente";
		FechaInicio = DateTime.Today;
	}

	public PuestoDocenteViewModel(PuestoResponse puestoDocenteResponse)
	{
		if (puestoDocenteResponse is not null)
		{
			_puestoDocenteResponse = puestoDocenteResponse;
			_upsert = UpsertType.Update.ToString();

			PuestoID = _puestoDocenteResponse.PuestoID;
			Estado = _puestoDocenteResponse.Estado;
			Posicion = _puestoDocenteResponse.Posicion;
			FechaInicio = _puestoDocenteResponse.FechaInicio;
			FechaFin = _puestoDocenteResponse.FechaFin;
			EsEventual = _puestoDocenteResponse.EsEventual;
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
			SetProperty(ref _fechaInicio, value, true);
			if (EsEventual)
			{
				FechaFin = FechaInicio;
				ValidateProperty(FechaFin, nameof(FechaFin));
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
			SetProperty(ref _fechaFin, value, true);
		}
	}

	public bool EsEventual
	{
		get
		{
			return _esEventual;
		}

		set
		{
			SetProperty(ref _esEventual, value);
			FechaFin = EsEventual ? FechaInicio : null;
		}
	}
}