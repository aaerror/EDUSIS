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

	// Sin ErrorMessage, DataAnnotations contesta en inglés ("The FechaNacimiento field is
	// required."). Daba igual mientras el motivo vivía sólo en un ToolTip que nadie abría;
	// ahora se lee debajo del campo, en una pantalla que está entera en español.
	[Required(ErrorMessage="Se debe ingresar la fecha de nacimiento.")]
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

	/// <summary>
	/// Valida todos los campos del bloque, incluidos los que el usuario nunca tocó.
	/// </summary>
	/// <remarks>
	/// <c>ObservableValidator</c> valida en el setter, así que un campo que nadie escribió no
	/// tiene errores: el formulario recién abierto está "sin errores" y vacío a la vez. Esto es
	/// lo que el paso llama al intentar avanzar, para que lo que falta se marque donde falta.
	/// <c>ValidateAllProperties</c> es protegido, y por eso cada bloque lo expone así.
	/// </remarks>
	public void Validar() =>
		ValidateAllProperties();
}