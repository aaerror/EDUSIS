using Domain.Shared;
using Domain.Usuarios;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System;

namespace WPF_Desktop.Shared.Converters;

/// <summary>
/// Traduce la lista de <c>Id</c> de rol que trae <c>UsuarioResponse.RolesIDs</c> a sus descripciones,
/// separadas por coma: <c>[1, 3]</c> → <c>"Administrador, Docente"</c>.
/// </summary>
/// <remarks>
/// <para>
/// La traducción vive acá y no en el <c>Response</c> a propósito. Los roles viajan por <c>Id</c>
/// porque el Id es canónico y la descripción es <em>presentación</em>: vive en una columna
/// <c>varchar</c> no unicode, y como alimenta la autorización, degradarla sería perder permisos en
/// silencio. Decisión #6 de <c>docs/plan/rbac-y-seed.md</c>, explicada en el XML-doc de
/// <c>UsuarioResponse</c>. La presentación es justamente el lugar donde el Id vuelve a ser texto.
/// </para>
/// <para>
/// Un Id que no esté en el catálogo <strong>no revienta la grilla</strong>: se muestra como
/// <c>"#&lt;id&gt;"</c>. Una fila de <c>usuario_rol</c> apuntando a un rol inexistente es un dato
/// corrupto, y el lugar donde el administrador tiene que poder verlo es esta pantalla — no una
/// excepción dentro de un binding, que WPF se traga y deja la celda vacía.
/// </para>
/// </remarks>
internal class RolesConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not IEnumerable<int> rolesIDs)
		{
			return string.Empty;
		}

		var descripciones = rolesIDs.Select(Describir).ToList();

		return descripciones.Count is 0
			? "Sin roles"
			: string.Join(", ", descripciones);
	}

	/// <remarks>
	/// <c>internal</c> y no privada porque <see cref="RolConverter"/> traduce de a un Id y la
	/// tolerancia al rol desconocido —el <c>"#&lt;id&gt;"</c> en vez de la excepción— tiene que ser la
	/// misma en los dos lugares.
	/// </remarks>
	internal static string Describir(int rolID)
	{
		try
		{
			return Enumeration.FromValue<Rol>(rolID).Descripcion;
		}
		catch (Exception)
		{
			return $"#{ rolID }";
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException("Los roles se asignan y se quitan por caso de uso, no por binding de dos vías.");
}
