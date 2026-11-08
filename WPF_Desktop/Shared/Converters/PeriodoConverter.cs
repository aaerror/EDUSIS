using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

/// <summary>
/// El nombre de un miembro de <c>Periodo</c> al texto que se lee en pantalla.
/// </summary>
/// <remarks>
/// Existe para la lista desplegable de la pantalla de calificaciones, que se llena con
/// <c>Enum.GetNames</c>: sus elementos son los nombres crudos —"PrimerCuatrimestre"— y lo que el
/// usuario tiene que leer es "1° cuatrimestre". El valor seleccionado sigue siendo el nombre, que
/// es lo que viaja en el request.
/// </remarks>
internal class PeriodoConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		Periodos.Decir(value as string);

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException("El período viaja como nombre del miembro del enum; la lista ya selecciona ese valor.");
}
