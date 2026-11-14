namespace Domain.Cursos;

/// <summary>
/// Cómo se ubica la edad de un alumno respecto de la edad esperada para el grado al que se
/// inscribe.
/// </summary>
/// <remarks>
/// Es un veredicto y no una excepción porque la regla <strong>advierte, no bloquea</strong>: una
/// trayectoria escolar atípica —repetición, reingreso, cambio de provincia, escolarización tardía—
/// es legítima, y el sistema no tiene con qué distinguirla de un error de carga. Quien decide es
/// la persona que inscribe; el dominio sólo nombra el desvío.
/// </remarks>
public enum EncuadreEtario
{
	/// <summary>La edad cae dentro de la banda normal para el grado.</summary>
	Encuadrado = 0,

	/// <summary>Por debajo de la banda: el alumno es más chico de lo que el grado espera.</summary>
	PorDebajoDeLaEdadEsperada = 1,

	/// <summary>Por encima de la banda: sobreedad que supera la tolerada.</summary>
	ConSobreedad = 2
}
