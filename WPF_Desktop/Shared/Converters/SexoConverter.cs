using Domain.Personas;
using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

internal class SexoConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (targetType.IsEnum)
		{
			return Enum.GetName(typeof(Sexo), value);
		}

		if (!string.IsNullOrWhiteSpace(value.ToString()))
		{
			var result = Enum.TryParse<Sexo>(value.ToString(), out Sexo sexo);
			if (result)
			{
				return sexo;
			}
		}

		//return Enum.Parse(typeof(Sexo), value.ToString());
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Enum.Parse(typeof(Sexo), value.ToString());
	}
}