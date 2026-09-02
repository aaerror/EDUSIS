using System.ComponentModel.DataAnnotations;
using System;

namespace WPF_Desktop.Validations;

internal class DateBeforeThanToday : ValidationAttribute
{
	public override string FormatErrorMessage(string name) =>
		"La fecha debe ser anterior al día de hoy.";

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