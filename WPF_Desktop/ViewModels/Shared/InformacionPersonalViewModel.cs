using CommunityToolkit.Mvvm.ComponentModel;
using Core.Shared.DTOs.Personas.Responses;
using System.ComponentModel.DataAnnotations;
using System;
using WPF_Desktop.Validations;

namespace WPF_Desktop.ViewModels.Shared;

internal partial class InformacionPersonalViewModel : ObservableValidator
{
	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar el apellido.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _apellido;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar el nombre.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _nombre;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar el número de documento.")]
	[RegularExpression(@"^(\d){8}$", ErrorMessage="Formato de número de documento inválido.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _documentoNacionalIdentidad;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe seleccionar el sexo.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _sexo;

	[Required]
	[DateBeforeThanToday]
	[DataType(DataType.Date, ErrorMessage="Formato de fecha de nacimiento inválida.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private DateTime _fechaNacimiento;

	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe ingresar la nacionalidad.")]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _nacionalidad;


	public InformacionPersonalViewModel() { }

	public InformacionPersonalViewModel(DatosPersonalesResponse informacionPersonalResponse)
	{
		if (informacionPersonalResponse is not null)
		{
			Apellido = informacionPersonalResponse.Apellido;
			Nombre = informacionPersonalResponse.Nombre;
			DocumentoNacionalIdentidad = informacionPersonalResponse.DNI;
			//_sexo = Enum.Parse<Sexo>(informacionPersonalResponse.Sexo);
			Sexo = informacionPersonalResponse.Sexo;
			FechaNacimiento = informacionPersonalResponse.FechaNacimiento.Date;
			Nacionalidad = informacionPersonalResponse.Nacionalidad;
		} 
	}
}