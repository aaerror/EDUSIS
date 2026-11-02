using System.ComponentModel.DataAnnotations;
using System;

namespace WPF_Desktop.Validations;

internal class DateAfterOrEqual : ValidationAttribute
{
	public string PropertyName { get; }


	public DateAfterOrEqual(string propertyName)
	{
		PropertyName = propertyName;
	}
	
	/// <remarks>
	/// UNA FECHA AUSENTE NO ES UNA FECHA INVÁLIDA, y este validador se caía con ella: hacía
	/// <c>((IComparable) value).CompareTo(...)</c> sin mirar si <paramref name="value"/> era nulo, así
	/// que validar la fecha de fin de un titular —que es por tiempo indeterminado y la tiene en
	/// blanco— tiraba <c>NullReferenceException</c> desde adentro del validador. No se notaba porque
	/// el único lugar que lo disparaba lo hacía justo después de asignarle un valor.
	///
	/// Que el campo sea obligatorio es otra regla y tiene otro dueño: <c>[Required]</c> para el
	/// formulario, y el dominio para la verdad. Acá sólo se compara el orden de dos fechas, y si falta
	/// alguna no hay nada que comparar.
	/// </remarks>
	protected override ValidationResult IsValid(object value, ValidationContext validationContext)
	{
		if (value is null)
		{
			return ValidationResult.Success;
		}

		var instance = validationContext.ObjectInstance;
		var propiedad = instance?.GetType().GetProperty(PropertyName);
		var dateStart = propiedad?.GetValue(instance);

		if (dateStart is null)
		{
			return ValidationResult.Success;
		}

		if (((IComparable) value).CompareTo(dateStart) < 0)
		{
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
		}

		return ValidationResult.Success;
	}

	public override string FormatErrorMessage(string name) =>
		$"La fecha { name } no debe ser anterior a la fecha de inicio.";
}