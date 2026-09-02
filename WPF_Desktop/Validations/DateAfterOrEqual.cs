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
	
	protected override ValidationResult IsValid(object value, ValidationContext validationContext)
	{
		var instance = validationContext.ObjectInstance;
		var dateStart = instance.GetType().GetProperty(PropertyName).GetValue(instance);

		if (((IComparable) value).CompareTo(dateStart) < 0)
		{
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
		}

		return ValidationResult.Success;
	}

	public override string FormatErrorMessage(string name) =>
		$"La fecha { name } no debe ser anterior a la fecha de inicio.";
}