using Domain.Shared;

namespace Domain.Cursos;

/// <summary>
/// Dueño de la regla que relaciona la edad de un alumno con el grado al que se inscribe.
/// </summary>
/// <remarks>
/// <para>
/// Es una regla <strong>entre agregados</strong> y por eso necesita un dueño explícito, igual que
/// <see cref="Curriculas.TopesCurriculares"/>: la edad vive en <c>Alumno</c>, el grado y el nivel
/// en <c>Curso</c>, y las raíces se referencian sólo por identificador, así que ninguna de las dos
/// tiene la otra delante. La capa de aplicación aporta los datos y esta política decide.
/// </para>
/// <para>
/// <strong>Advierte, no bloquea</strong> (decisión del 2026-11-12): devuelve un
/// <see cref="EncuadreEtario"/> en lugar de lanzar. La inscripción se registra siempre y el desvío
/// viaja hasta la pantalla, que lo muestra. El motivo es que la regla correcta depende de la
/// trayectoria académica del alumno —repetición, reingreso, escolarización tardía— y el dominio
/// todavía no la modela: bloquear con la información disponible dejaría afuera trayectorias
/// legítimas sin forma de forzar la inscripción.
/// </para>
/// </remarks>
public static class EdadEscolar
{
	// La edad de ingreso al nivel es lo único que hace falta para derivar la edad esperada de
	// cualquier grado, porque `Grado` es ordinal. La DURACIÓN del nivel no entra en la cuenta: que
	// la primaria sea de 6 o de 7 años según la provincia no cambia que primero sea a los 6, sólo
	// hasta qué grado llega el enum.
	public const int EDAD_INGRESO_PRIMARIA = 6;

	// Las dos constantes están acopladas: una primaria de 6 años pone la secundaria a los 12, y
	// una de 7 la pone a los 13. Es un dato de la institución y no del alumno. Con la tolerancia
	// de sobreedad en 4 años, elegir 12 o 13 no cambia ningún veredicto.
	public const int EDAD_INGRESO_SECUNDARIA = 12;

	/// <summary>
	/// Años de sobreedad que se toleran antes de advertir.
	/// </summary>
	/// <remarks>
	/// <strong>Valor provisional.</strong> No sale de una norma: es el umbral que frena los casos
	/// groseros —un alumno de 12 en primero de primaria, uno de 18 en primero de secundaria, los
	/// dos +6— dejando pasar dos repeticiones o una repetición más un reingreso. El criterio real
	/// depende de la trayectoria académica, que este dominio no modela todavía; cuando la modele,
	/// esta constante es lo primero que hay que revisar.
	/// </remarks>
	public const int SOBREEDAD_TOLERADA = 4;

	/// <summary>
	/// Años por debajo de la edad esperada que se consideran normales.
	/// </summary>
	/// <remarks>
	/// <strong>No es tolerancia de criterio sino corrección de medición</strong>, y por eso vale 1
	/// y no 0 aunque adelantar año no sea una trayectoria común: <c>DatosPersonales.Edad()</c> se
	/// calcula contra la fecha de hoy y no contra el inicio del ciclo lectivo, así que un alumno
	/// nacido en diciembre tiene un año menos que su cohorte durante casi todo el año. Sin esta
	/// banda, el veredicto dependería del mes en que se mira y marcaría como desvío a buena parte
	/// de la división.
	/// </remarks>
	public const int ANIOS_DE_VARIANZA_POR_MES_DE_NACIMIENTO = 1;

	/// <summary>Edad que se espera de un alumno en ese nivel y grado.</summary>
	public static int EdadEsperada(NivelEducativo nivel, Grado grado) =>
		EdadDeIngreso(nivel) + ((int)grado - 1);

	/// <summary>Edad de ingreso al primer grado del nivel.</summary>
	public static int EdadDeIngreso(NivelEducativo nivel) =>
		nivel switch
		{
			NivelEducativo.Primaria => EDAD_INGRESO_PRIMARIA,
			NivelEducativo.Secundaria => EDAD_INGRESO_SECUNDARIA,
			_ => throw new ArgumentOutOfRangeException(nameof(nivel), nivel, "Nivel educativo desconocido.")
		};

	/// <summary>
	/// Ubica <paramref name="edad"/> respecto de la banda normal del grado. No lanza: el llamador
	/// decide qué hacer con el veredicto.
	/// </summary>
	public static EncuadreEtario Evaluar(int edad, NivelEducativo nivel, Grado grado)
	{
		var esperada = EdadEsperada(nivel, grado);

		if (edad < esperada - ANIOS_DE_VARIANZA_POR_MES_DE_NACIMIENTO)
		{
			return EncuadreEtario.PorDebajoDeLaEdadEsperada;
		}

		if (edad > esperada + SOBREEDAD_TOLERADA)
		{
			return EncuadreEtario.ConSobreedad;
		}

		return EncuadreEtario.Encuadrado;
	}

	/// <summary>
	/// Años de desvío respecto de la edad esperada: positivo si el alumno es mayor, negativo si es
	/// menor, cero si coincide. Es el dato que la pantalla necesita para redactar la advertencia,
	/// y se calcula contra la edad esperada y no contra el borde de la banda.
	/// </summary>
	public static int AniosDeDesvio(int edad, NivelEducativo nivel, Grado grado) =>
		edad - EdadEsperada(nivel, grado);
}
