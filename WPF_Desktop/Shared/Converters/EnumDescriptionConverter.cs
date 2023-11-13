using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace WPF_Desktop.Shared.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    private string GetEnumDescription(Enum obj)
    {
        FieldInfo fieldInfo = obj.GetType().GetField(obj.ToString());

        object[] attributeArray = fieldInfo.GetCustomAttributes(false);

        if (attributeArray.Length == 0)
        {
            return obj.ToString();
        }
        else
        {
            DescriptionAttribute attrib = null;

            foreach (var attribute in attributeArray)
            {
                if (attribute is DescriptionAttribute)
                {
                    attrib = attribute as DescriptionAttribute;
                }
            }

            if (attrib != null)
            {
                return attrib.Description;
            }

            return obj.ToString();
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Enum enumeration = (Enum) value;

        string description = GetEnumDescription(enumeration);

        return description;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.Empty;
    }
}
