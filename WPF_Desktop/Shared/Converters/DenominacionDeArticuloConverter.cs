using Domain.Licencias;
using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

/// <summary>
/// Muestra un <see cref="Articulo"/> con su nombre legal ("Fallecimiento de hermano o suegro") en
/// lugar del identificador del enumerado. Sólo de ida: el combo enlaza el enumerado directo.
/// </summary>
internal class DenominacionDeArticuloConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		value is Articulo articulo ? PlazoLegal.De(articulo).Denominacion : string.Empty;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException("La denominación de un artículo no se convierte de vuelta.");
}
