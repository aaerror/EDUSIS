using System.ComponentModel.DataAnnotations;
using System;

namespace WPF_Desktop.Validations;

internal class DateBeforeThanToday : ValidationAttribute
{
	// El mensaje decía "debe ser anterior al día de hoy", pero la regla de abajo acepta HOY:
	// sólo rechaza lo posterior. Quien leía el error y corregía a ayer estaba obedeciendo una
	// regla que la aplicación no tiene. El mensaje ahora dice lo que la regla hace.
	public override string FormatErrorMessage(string name) =>
		"La fecha no puede ser posterior a hoy.";

	protected override ValidationResult IsValid(object objValue, ValidationContext validationContext)
	{
		var dateValue = objValue as DateTime? ?? new DateTime();

		if (dateValue.Date > DateTime.Now.Date)
		{
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
		}

		return ValidationResult.Success;
	}
}