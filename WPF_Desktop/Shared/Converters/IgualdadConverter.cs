using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

/// <summary>
/// El valor coincide con <c>ConverterParameter</c>. Compara por texto, sin distinguir
/// mayúsculas.
/// </summary>
/// <remarks>
/// Existe para que el rail pueda marcar la sección abierta: <c>MainViewModel.SeccionActual</c>
/// es el nombre de la sección, y cada entrada del menú necesita saber si ese nombre es el
/// suyo. Un <c>DataTrigger</c> compara un enlace contra un valor fijo, nunca dos enlaces entre
/// sí, así que sin esto la alternativa era repetir un estilo por entrada.
/// </remarks>
internal class IgualdadConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		string.Equals(value as string, parameter as string, StringComparison.OrdinalIgnoreCase);

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException("La comparación no se traduce de vuelta a un valor.");
}
