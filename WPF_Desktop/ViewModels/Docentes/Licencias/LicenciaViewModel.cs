using CommunityToolkit.Mvvm.ComponentModel;
using Core.ServicioLicencias.DTOs.Responses;
using Domain.Licencias;
using System.ComponentModel.DataAnnotations;
using System;
using WPF_Desktop.Validations;

namespace WPF_Desktop.ViewModels.Docentes.Licencias;

internal partial class LicenciaViewModel : ObservableValidator
{
	#region Response
	private readonly LicenciaResponse _licenciaResponse;
	#endregion

	[ObservableProperty]
	private Guid _licenciaID = Guid.Empty;

	[Required]
	[EnumDataType(typeof(Articulo))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _articulo = string.Empty;

	[Required]
	[EnumDataType(typeof(Estado))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _estado = string.Empty;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la cantidad de días de licencia.")]
	[Range(0, 365, ErrorMessage="La cantidad de días de licencia debe estar comprendida entre un día hasta un año.")]
	[RegularExpression(@"^(\d){1,3}$", ErrorMessage="Formato inválido. La cantidad de días debe ser un número.", MatchTimeoutInMilliseconds=2000)]
	[NotifyPropertyChangedFor(nameof(FechaFin))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private int _dias;

	[ObservableProperty]
	private bool _esIndefinida;

	[Required(ErrorMessage="Se debe ingresar la fecha de inicio de la licencia.")]
	[DataType(DataType.Date, ErrorMessage="Formato inválido. Se debe ingresar una fecha en el siguiente formato dd/mm/aaaa.")]
	[DateBeforeThanToday(ErrorMessage="La fecha de inicio no ser superior al día de hoy.")]
	[NotifyPropertyChangedFor(nameof(FechaFin))]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private DateTime _fechaInicio = DateTime.Today;

	[ObservableProperty]
	public DateTime? _fechaFin;

	[MaxLength(250, ErrorMessage="El detalle de observación es muy extensa.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string? _observacion = string.Empty;


	public LicenciaViewModel()
	{
		Estado = "Pendiente";
		EsIndefinida = false;
	}

	public LicenciaViewModel(LicenciaResponse licenciaResponse)
		: this()
	{
		if (licenciaResponse is not null)
		{
			_licenciaResponse = licenciaResponse;

			LicenciaID = _licenciaResponse.LicenciaID;
			Articulo = _licenciaResponse.Articulo;
			Estado = _licenciaResponse.Estado;
			FechaInicio = _licenciaResponse.FechaInicio;
			FechaFin = _licenciaResponse.FechaFin;
			EsIndefinida = !_licenciaResponse.FechaFin.HasValue;
			Dias = _licenciaResponse.Dias;
			Observacion = _licenciaResponse.Observacion;
		}
	}


	partial void OnEsIndefinidaChanged(bool value) =>
		Dias = value ? 0 : 1;

	partial void OnDiasChanged(int value) =>
		FechaFin = Dias > 0 ? FechaInicio.AddDays(value) : null;
}