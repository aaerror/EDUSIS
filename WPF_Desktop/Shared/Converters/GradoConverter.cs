using Domain.Cursos;
using System.Globalization;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

internal class GradoConverter : IValueConverter
{
	/// <remarks>
	/// Armaba el texto por su cuenta con el valor numérico del enum y además lo devolvía con un
	/// espacio adelante. Ahora lo pide a <see cref="Grados"/>, que lee el texto que el propio
	/// <see cref="Grado"/> declara, para que la pantalla no tenga su tabla de equivalencias.
	/// </remarks>
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		Grados.Decir(value as string);

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var isDefined = Enum.IsDefined(typeof(Grado), value);
		if (isDefined)
		{
			return Enum.GetName(typeof(Grado), value);
		}

		return string.Empty;
	}
}