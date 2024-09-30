using Domain.Materias.CargosDocentes;
using System;
using System.Globalization;
using System.Windows.Data;

namespace WPF_Desktop.Shared.Converters;

public class CargoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType.IsEnum)
        {
            return Enum.GetName(typeof(Cargo), value);
        }

        return Enum.Parse(typeof(Cargo), value.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Enum.Parse(typeof(Cargo), value.ToString());
    }
}
