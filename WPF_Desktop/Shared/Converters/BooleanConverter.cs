using System;
using System.Windows.Data;
using System.Globalization;
using System.Collections.Generic;

namespace WPF_Desktop.Shared.Converters;

public class BooleanConverter<T> : IValueConverter
{
	public T FalseValue { get; set; }
	public T TrueValue { get; set; }


	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is null ? FalseValue : ((bool) value ? TrueValue : FalseValue);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		// return value != null ? value.Equals(TrueValue) : false;

		// Note: this implementation precludes the use of "null" as the
		// value for TrueValue. Probably not an issue in 99.94% of all cases,
		// but something to consider, if one is looking to make a truly 100%
		// general-purpose class here.
		return value != null && EqualityComparer<T>.Default.Equals((T) value, TrueValue);
	}
}