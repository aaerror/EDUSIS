using Domain.Docentes.Puestos;
using System;
using System.Globalization;
using System.Windows.Data;

namespace WPF_Desktop.Shared.Converters;

internal class PosicionConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not null)
		{
			Enum.TryParse(typeof(Posicion), value.ToString(), true, out var posicion);

			return posicion;
		}

		return string.Empty;
	}


	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not null)
		{
			return Enum.Parse(typeof(Posicion), value.ToString());
		}

		return string.Empty;
	}
}