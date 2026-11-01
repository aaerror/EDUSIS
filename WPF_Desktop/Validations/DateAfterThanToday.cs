using System.ComponentModel.DataAnnotations;
using System;

namespace WPF_Desktop.Validations;

internal class DateAfterThanToday : ValidationAttribute
{
	// Misma corrección que en DateBeforeThanToday, al revés: la regla acepta HOY y sólo rechaza
	// lo anterior, así que el mensaje no puede exigir una fecha posterior a hoy. Importa acá
	// porque el alta de un puesto arranca justamente en la fecha de hoy.
	public override string FormatErrorMessage(string name) =>
		"La fecha no puede ser anterior a hoy.";

	protected override ValidationResult IsValid(object objValue, ValidationContext validationContext)
	{
		var dateValue = objValue as DateTime? ?? new DateTime();

		if (dateValue.Date < DateTime.Now.Date)
		{
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
		}

		return ValidationResult.Success;
	}
}