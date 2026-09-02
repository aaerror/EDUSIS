using Domain.Personas.Domicilios;
using System.Globalization;
using System.Windows.Data;
using System;
using Domain.Personas;

namespace WPF_Desktop.Shared.Converters;

internal class ViviendaConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (!string.IsNullOrWhiteSpace(value.ToString()))
		{
			var result = Enum.TryParse<Vivienda>(value.ToString(), out Vivienda vivienda);
			if (result)
			{
				return vivienda;
			}
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Enum.Parse(typeof(Vivienda), value.ToString());
	}
}