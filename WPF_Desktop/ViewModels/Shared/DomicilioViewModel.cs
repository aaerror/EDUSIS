using CommunityToolkit.Mvvm.ComponentModel;
using Core.Shared.DTOs.Personas.Responses;
using System.ComponentModel.DataAnnotations;

namespace WPF_Desktop.ViewModels.Shared;

internal partial class DomicilioViewModel : ObservableValidator
{
	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la calle.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _calle;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la altura.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _altura;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la vivienda.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _vivienda;

	[MaxLength(120, ErrorMessage="Las observaciones no deben superar los 120 caracteres.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _observaciones;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la localidad.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _localidad;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la provincia.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _provincia;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar el país.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _pais;


	public DomicilioViewModel(DomicilioResponse domicilioResponse)
	{
		if (domicilioResponse is not null)
		{
			Calle = domicilioResponse.Calle;
			Altura = domicilioResponse.Altura;
			//TODO:Verificar string
			//Vivienda = Enum.Parse<Vivienda>(domicilioResponse.Vivienda);
			Vivienda = domicilioResponse.Vivienda;
			Localidad = domicilioResponse.Localidad;
			Provincia = domicilioResponse.Provincia;
			Pais = domicilioResponse.Pais;
			Observaciones = domicilioResponse.Observacion;
		}
	}
}