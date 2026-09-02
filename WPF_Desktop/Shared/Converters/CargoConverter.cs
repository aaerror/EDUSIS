using Domain.Curriculas.Materias.CargosDocentes;
using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

internal class CargoConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var data = value as string;
		if (!string.IsNullOrWhiteSpace(data))
		{
			var result = Enum.TryParse<Cargo>(data, out var cargo);
			if (result)
			{
				return cargo;
			}
		}

		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var data = value as Enum;
		if (data is not null)
		{
			var isDefined = Enum.IsDefined(typeof(Cargo), value);
			if (isDefined)
			{
				return data.ToString();
			}

			return string.Empty;
		}

		return string.Empty;
	}
}