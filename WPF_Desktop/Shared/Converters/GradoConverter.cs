using Domain.Cursos;
using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

internal class GradoConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var data = value as string;
		if (!string.IsNullOrWhiteSpace(data))
		{
			var result = Enum.TryParse<Grado>(data, out var grado);
			if (result)
			{
				return $" { (int) grado }° Año";
			}
		}

		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var isDefined = Enum.IsDefined(typeof(Grado), value);
		if (isDefined)
		{
			return Enum.GetName(typeof(Grado), value);
		}

		return string.Empty;
	}
}