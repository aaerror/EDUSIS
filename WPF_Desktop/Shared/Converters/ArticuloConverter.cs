using Domain.Licencias;
using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

internal class ArticuloConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var data = value as string;
		if (!string.IsNullOrWhiteSpace(data))
		{
			var result = Enum.TryParse<Articulo>(data, out var articulo);
			if (result)
			{
				return articulo;
			}
		}

		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var isDefined = Enum.IsDefined(typeof(Articulo), value);
		if (isDefined)
		{
			return Enum.GetName(typeof(Articulo), value);
		}
			
		return string.Empty;
	}
}