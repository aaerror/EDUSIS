using Domain.Cursos;
using System.ComponentModel;
using System.Reflection;
using System;

namespace WPF_Desktop.Shared;

/// <summary>
/// Cómo se dice un grado en pantalla.
/// </summary>
/// <remarks>
/// <para>
/// EL SÍMBOLO DE GRADO ESTABA PEGADO A UNA PALABRA QUE YA ERA EL ORDINAL. <see cref="Grado"/>
/// es un enum cuyos miembros se llaman <c>Primero</c>, <c>Segundo</c>…, y los ViewModels
/// guardan ese nombre como cadena. Diecisiete lugares le concatenaban el símbolo —
/// <c>$"{ Curso }° año"</c>, <c>StringFormat={}{0}° Año</c>— y el resultado que leía el
/// usuario era "Primero° año" y "Primero° PRIMARIA · DIVISIONES".
/// </para>
/// <para>
/// El texto correcto ya estaba declarado: cada miembro de <see cref="Grado"/> trae su
/// <see cref="DescriptionAttribute"/> con "1° Año", y <c>Core</c> lo lee así para los
/// documentos —<c>ContextoAcademico.Describir</c>, fijado por sus pruebas en "3° Año"—. Esto
/// es lo mismo para la pantalla, partiendo del nombre, que es lo que tienen los ViewModels.
/// </para>
/// <para>
/// Queda una segunda lectura del mismo atributo, la de <c>Core</c>. Unificarlas es mover
/// <c>Describir</c> a un lugar que las dos capas puedan ver, y eso es una decisión de
/// arquitectura que excede a este arreglo.
/// </para>
/// </remarks>
internal static class Grados
{
	/// <summary>"1° Año" a partir del nombre del miembro del enum, que es lo que guardan los ViewModels.</summary>
	/// <returns>Cadena vacía si el nombre no es un <see cref="Grado"/>: en pantalla es un hueco, no una excepción.</returns>
	public static string Decir(string? nombre) =>
		Enum.TryParse<Grado>(nombre, out var grado) ? Decir(grado) : string.Empty;

	/// <inheritdoc cref="Decir(string?)"/>
	public static string Decir(Grado grado)
	{
		var miembro = typeof(Grado).GetField(grado.ToString());
		var descripcion = miembro?.GetCustomAttribute<DescriptionAttribute>();

		return descripcion?.Description ?? grado.ToString();
	}
}
