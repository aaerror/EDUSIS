using Domain.Licencias;
using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

internal class LicenciaEstadoConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var data = value as string;
		if (!string.IsNullOrWhiteSpace(data))
		{
			var result = Enum.TryParse<Estado>(data, out var estado);
			if (result)
			{
				return estado;
			}
		}

		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var isDefined = Enum.IsDefined(typeof(Estado), value);
		if (isDefined)
		{
			return Enum.GetName(typeof(Estado), value);
		}

		return string.Empty;
	}
}