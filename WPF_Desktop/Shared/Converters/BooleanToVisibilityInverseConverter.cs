using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System;

namespace WPF_Desktop.Shared.Converters;

/// <summary>
/// <c>true</c> oculta, <c>false</c> muestra. Es el complemento de
/// <see cref="BooleanToVisibilityConverter"/>, que WPF no trae.
/// </summary>
/// <remarks>
/// Sirve para los estados vacíos: la pantalla de bienvenida se muestra justamente cuando
/// <c>HayContenido</c> es falso. La alternativa —exponer una propiedad <c>Visibility</c> desde
/// el ViewModel— mete un tipo de presentación en la capa que no debería conocerlo.
/// </remarks>
internal class BooleanToVisibilityInverseConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		value is true ? Visibility.Collapsed : Visibility.Visible;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		value is Visibility.Collapsed or Visibility.Hidden;
}
