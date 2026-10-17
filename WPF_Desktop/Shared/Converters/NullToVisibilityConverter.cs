using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System;

namespace WPF_Desktop.Shared.Converters;

/// <summary>
/// Hay valor, se muestra. Con <c>ConverterParameter="inverso"</c>, al revés.
/// </summary>
/// <remarks>
/// Es el par que sostiene el patrón índice + ficha: la ficha se muestra cuando hay una fila
/// elegida y, en el mismo lugar, el texto de "elegí algo" cuando no la hay.
///
/// Reemplaza al truco que usaban las vistas: enlazar <c>IsEnabled</c> al objeto seleccionado
/// y después <c>Visibility</c> al propio <c>IsEnabled</c> con un
/// <c>BooleanToVisibilityConverter</c>. Eso funcionaba, pero deshabilitaba el panel además de
/// ocultarlo, y dejaba la intención escondida en dos enlaces encadenados.
/// </remarks>
internal class NullToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		bool hayValor = value is not null;

		if (parameter is string texto && texto.Equals("inverso", StringComparison.OrdinalIgnoreCase))
		{
			hayValor = !hayValor;
		}

		return hayValor ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException("La visibilidad no se traduce de vuelta a un valor.");
}
