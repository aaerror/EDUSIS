using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

/// <summary>
/// Traduce <strong>un</strong> <c>Id</c> de rol a su descripción: <c>4</c> → <c>"Secretaria"</c>.
/// </summary>
/// <remarks>
/// <para>
/// El hermano de a uno de <see cref="RolesConverter"/>, que traduce la lista entera a un texto con
/// comas. Existe porque la ficha de la cuenta dibuja <strong>una insignia por rol</strong> y no una
/// cadena: un <c>ItemsControl</c> sobre <c>UsuarioResponse.RolesIDs</c> le pasa a su plantilla un
/// <c>int</c> suelto, que es justo lo que el otro conversor no acepta.
/// </para>
/// <para>
/// Comparten la traducción —<see cref="RolesConverter.Describir"/>—, así que el Id desconocido se
/// muestra como <c>"#&lt;id&gt;"</c> en los dos lugares y no revienta el enlace. Las razones de que
/// los roles viajen por Id están en el XML-doc de <c>UsuarioResponse</c>.
/// </para>
/// </remarks>
internal class RolConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		value is int rolID ? RolesConverter.Describir(rolID) : string.Empty;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException("Los roles se asignan y se quitan por caso de uso, no por binding de dos vías.");
}
