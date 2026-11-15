namespace Domain.Shared;

/// <summary>
/// Conversión única de texto a enumerado del dominio. Cada entidad la envuelve con la excepción
/// propia de su regla.
/// </summary>
internal static class Enumerados
{
	/// <remarks>
	/// <c>Enum.TryParse</c> solo no alcanza: acepta cualquier número ("99" da <c>(Cargo)99</c>),
	/// que se descarta con <see cref="Enum.IsDefined{TEnum}(TEnum)"/>, y combinaciones separadas
	/// por coma que hace OR de los valores. Esas se rechazan aparte porque el OR puede caer en un
	/// valor definido: "Primero, Segundo" da 1 | 2 = 3, que es <c>Grado.Tercero</c>. No distingue
	/// mayúsculas, como hacían los servicios de <c>Core</c> antes de que el parseo bajara al dominio.
	/// </remarks>
	public static TEnum Convertir<TEnum>(string? valor, Func<string, Exception> excepcion)
		where TEnum : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(valor)
			|| valor.Contains(',')
			|| !Enum.TryParse<TEnum>(valor.Trim(), ignoreCase: true, out var resultado)
			|| !Enum.IsDefined(resultado))
		{
			throw excepcion(valor ?? string.Empty);
		}

		return resultado;
	}
}
