using Domain.Cursantes.Calificaciones;
using System.ComponentModel;
using System.Reflection;
using System;

namespace WPF_Desktop.Shared;

/// <summary>
/// Cómo se dice en pantalla un período del ciclo lectivo, y cómo se nombra una instancia completa.
/// </summary>
/// <remarks>
/// Mismo mecanismo y mismo motivo que <see cref="Grados"/>: el texto legible lo declara el propio
/// <see cref="Periodo"/> con su <see cref="DescriptionAttribute"/>, y <c>Core</c> lo lee igual para
/// los documentos. Los ViewModels guardan el NOMBRE del miembro como cadena —es lo que viaja en los
/// DTOs—, así que la entrada de la conversión es ese nombre y no el enum.
/// </remarks>
internal static class Periodos
{
	/// <summary>"1° cuatrimestre" a partir del nombre del miembro del enum.</summary>
	/// <returns>Cadena vacía si el nombre no es un <see cref="Periodo"/>: en pantalla es un hueco, no una excepción.</returns>
	public static string Decir(string? nombre) =>
		Enum.TryParse<Periodo>(nombre, out var periodo) ? Decir(periodo) : string.Empty;

	/// <inheritdoc cref="Decir(string?)"/>
	public static string Decir(Periodo periodo)
	{
		var miembro = typeof(Periodo).GetField(periodo.ToString());
		var descripcion = miembro?.GetCustomAttribute<DescriptionAttribute>();

		return descripcion?.Description ?? periodo.ToString();
	}

	/// <summary>
	/// La instancia completa: el período, y si es el recuperatorio de ese período.
	/// </summary>
	/// <remarks>
	/// "1° cuatrimestre (rec.)" y no "Recuperatorio" a secas, que es lo que decía la grilla cuando
	/// el modelo tenía un solo eje: un ciclo admite cuatro recuperatorios y la celda no permitía
	/// distinguirlos.
	/// </remarks>
	public static string DecirInstancia(string? nombre, bool esRecuperatorio)
	{
		var periodo = Decir(nombre);

		return esRecuperatorio && periodo.Length > 0 ? $"{ periodo } (rec.)" : periodo;
	}
}
