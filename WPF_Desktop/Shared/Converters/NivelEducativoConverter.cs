using Domain.Cursos;
using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

internal class NivelEducativoConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var data = value as string;
		if (!string.IsNullOrWhiteSpace(data))
		{
			var result = Enum.TryParse<NivelEducativo>(data, out var nivelEducativo);
			if (result)
			{
				return nivelEducativo;
			}
		}

		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Enum.Parse(typeof(NivelEducativo), value.ToString());
	}
}