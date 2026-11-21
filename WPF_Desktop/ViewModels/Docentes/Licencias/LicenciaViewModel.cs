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

	// Igual que en el puesto: sin ErrorMessage el motivo sale en inglés, y ahora se lee debajo
	// del campo.
	// El enumerado y no su nombre: el combo lo enlaza sin conversor y el plazo legal se consulta
	// sin volver a parsear texto. Nulo mientras no se eligió.
	[Required(ErrorMessage="Se debe elegir el artículo de la licencia.")]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(Denominacion))]
	[NotifyPropertyChangedFor(nameof(ReferenciaLegal))]
	[NotifyPropertyChangedFor(nameof(AdmiteIndefinida))]
	[NotifyPropertyChangedFor(nameof(TieneDiasFijos))]
	[NotifyPropertyChangedFor(nameof(ResolucionAcademica))]
	[ObservableProperty]
	private Articulo? _articulo;

	[Required(ErrorMessage="Se debe indicar el estado de la licencia.")]
	[EnumDataType(typeof(Estado), ErrorMessage="El estado no es uno de los admitidos.")]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(EtiquetaEstado))]
	[NotifyPropertyChangedFor(nameof(EstaAprobada))]
	[ObservableProperty]
	private string _estado = string.Empty;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la cantidad de días de licencia.")]
	// 730 es el tope más largo del catálogo (enfermedad de largo tratamiento); el de cada artículo
	// lo valida el dominio al guardar.
	[Range(0, 730, ErrorMessage="La cantidad de días de licencia no puede superar los 730.")]
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

		// EsIndefinida ya nace en false, así que la asignación de arriba no dispara
		// OnEsIndefinidaChanged: sin esto el alta arrancaba con 0 días y sin fecha de fin en
		// una licencia que no es indefinida.
		Dias = 1;
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
			// Después de Dias: OnDiasChanged recalcula el fin desde el inicio, y en una licencia ya
			// guardada manda el que viene de la base (una finalizada antes de tiempo no es
			// inicio + días − 1 si se guardó con la cuenta anterior).
			FechaFin = _licenciaResponse.FechaFin;
			Observacion = _licenciaResponse.Observacion;
			Vencida = _licenciaResponse.Vencida;
		}
	}


	#region Plazo legal
	private PlazoLegal? Plazo =>
		Articulo is null ? null : PlazoLegal.De(Articulo.Value);

	/// <summary>Nombre legal del artículo, para el listado y los diálogos.</summary>
	public string Denominacion =>
		Plazo?.Denominacion ?? string.Empty;

	/// <summary>Norma y plazo del artículo elegido, como ayuda debajo del selector.</summary>
	public string ReferenciaLegal => Plazo switch
	{
		null => string.Empty,
		{ Tipo: TipoDePlazo.Fijo } p => $"{ p.Norma } · { p.Dias } días corridos",
		{ Tipo: TipoDePlazo.Maximo } p => $"{ p.Norma } · hasta { p.Dias } días corridos",
		var p => $"{ p.Norma } · sin plazo fijo"
	};

	public bool AdmiteIndefinida =>
		Plazo?.AdmiteIndefinida ?? false;

	public bool TieneDiasFijos =>
		Plazo?.Tipo == TipoDePlazo.Fijo;

	/// <summary>Si la decide sólo Secretaría Académica (exámenes, estudio encomendado, otras causas).</summary>
	public bool ResolucionAcademica =>
		Plazo?.ResolucionAcademica ?? false;

	// Al elegir el artículo se ajusta lo que la norma ya decide: los días de un plazo fijo y la
	// fecha de fin de uno que no admite quedar indefinido. El dominio igual lo valida al guardar.
	partial void OnArticuloChanged(Articulo? value)
	{
		if (Plazo is not { } plazo)
		{
			return;
		}

		if (!plazo.AdmiteIndefinida)
		{
			EsIndefinida = false;
		}

		if (plazo.Tipo == TipoDePlazo.Fijo)
		{
			Dias = plazo.Dias;
		}
	}
	#endregion

	#region Licencia aprobada
	/// <summary>Aprobada, con la fecha de fin pasada y sin finalizar: espera que se registre el reintegro.</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(EtiquetaEstado))]
	private bool _vencida;

	/// <summary>Lo que dice la insignia del listado: el estado, salvo la activa vencida.</summary>
	public string EtiquetaEstado =>
		Vencida ? "Vencida" : Estado;

	/// <summary>Aprobada y sin finalizar, vencida o no: admite reintegro, prórroga y cambio de duración.</summary>
	public bool EstaAprobada =>
		Estado.Equals(Enum.GetName(Domain.Licencias.Estado.Activa));
	#endregion

	partial void OnEsIndefinidaChanged(bool value) =>
		Dias = value ? 0 : 1;

	// La fecha de fin es el último día de licencia: inicio + días − 1, la misma cuenta que el
	// dominio. Una licencia de un día empieza y termina el mismo día.
	partial void OnDiasChanged(int value) =>
		FechaFin = Dias > 0 ? FechaInicio.AddDays(value - 1) : null;

	// NotifyPropertyChangedFor(FechaFin) sólo avisa; no recalcula. Sin esto, mover la fecha de
	// inicio dejaba a la vista la fecha de fin calculada con la anterior.
	partial void OnFechaInicioChanged(DateTime value) =>
		FechaFin = Dias > 0 ? value.AddDays(Dias - 1) : null;
}