using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

internal class InvertBooleanConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		bool original = (bool) value;
		return !original;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		bool original = (bool) value;
		return !original;
	}
}