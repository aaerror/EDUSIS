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

	// Los mensajes por omisión de DataAnnotations están en inglés y ahora se leen debajo del
	// campo, no en un ToolTip. Cada regla dice en español qué falta.
	[Required(ErrorMessage="Se debe indicar el estado del puesto.")]
	[EnumDataType(typeof(EstadoPuesto), ErrorMessage="El estado del puesto no es uno de los admitidos.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _estado = string.Empty;

	[Required(ErrorMessage="Se debe elegir la posición del docente.")]
	[EnumDataType(typeof(Posicion), ErrorMessage="La posición no es una de las admitidas.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _posicion = string.Empty;

	/*
		LAS REGLAS DE LAS DOS FECHAS ESTABAN EN EL CAMPO Y NO EN LA PROPIEDAD, así que no se
		aplicaban. ValidateProperty busca los ValidationAttribute declarados en la PROPIEDAD;
		el generador de [ObservableProperty] se encarga de mudarlos desde el campo, pero estas
		dos están escritas a mano y nadie los mudaba. Quedaban adornando un campo privado.

		Bajaron a FechaInicio y FechaFin, donde SetProperty(..., true) ya las iba a buscar.
	*/
	private DateTime _fechaInicio;

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

	[Required(ErrorMessage="Se debe ingresar la fecha de inicio del puesto docente.")]
	[DataType(DataType.Date, ErrorMessage="Formato inválido. Se debe ingresar una fecha en el siguiente formato dd/mm/aaaa.")]
	[DateAfterThanToday]
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

	[DataType(DataType.Date, ErrorMessage="Formato inválido. Se debe ingresar una fecha en el siguiente formato dd/mm/aaaa.")]
	[DateAfterOrEqual(nameof(FechaInicio), ErrorMessage="La fecha de finalización debe ser posterior o igual a la fecha de inicio.")]
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

	/// <inheritdoc cref="WPF_Desktop.ViewModels.Shared.InformacionPersonalViewModel.Validar"/>
	public void Validar() =>
		ValidateAllProperties();
}