using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System;

namespace WPF_Desktop.Shared.Converters;

/// <summary>
/// Hay texto, se muestra. Con <c>ConverterParameter="inverso"</c>, al revés.
/// </summary>
/// <remarks>
/// El par de <see cref="NullToVisibilityConverter"/> para las cadenas, que casi nunca llegan
/// nulas: llegan vacías. Un <c>string.Empty</c> no es nulo, así que <c>visibleSiHay</c> dejaba
/// visible un bloque sin una palabra adentro —es lo que hace hoy el aviso ámbar de
/// Documentación y Reportes, que se dibuja vacío en el primer pintado—. Las cadenas en blanco
/// cuentan como ausencia.
/// </remarks>
internal class TextoAVisibilidadConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		bool hayTexto = !string.IsNullOrWhiteSpace(value as string);

		if (parameter is string texto && texto.Equals("inverso", StringComparison.OrdinalIgnoreCase))
		{
			hayTexto = !hayTexto;
		}

		return hayTexto ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException("La visibilidad no se traduce de vuelta a un texto.");
}
