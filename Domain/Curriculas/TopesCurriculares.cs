using Domain.Curriculas.Exceptions;

namespace Domain.Curriculas;

/// <summary>
/// Dueño de las invariantes que gobiernan cuánto contenido admite una currícula.
/// </summary>
/// <remarks>
/// Estas dos reglas no pueden vivir en <see cref="Curricula"/>: las materias son una raíz de
/// agregado aparte y la currícula las referencia sólo por identificador, así que no tiene la
/// colección delante para contarla. Tampoco corresponden a <c>Materia</c>, que no conoce a sus
/// hermanas. Son reglas ENTRE agregados y necesitan un dueño explícito en el dominio — este.
/// La capa de aplicación aporta los totales (los obtiene del repositorio) y esta política decide;
/// así el cálculo queda donde se puede consultar y la regla donde pertenece.
/// </remarks>
public static class TopesCurriculares
{
	// Tope de espacios curriculares por currícula: 14 espacios es lo habitual en un año de
	// secundaria argentina (7mo a 12vo año, orientaciones incluidas).
	public const int MAXIMO_ESPACIOS_CURRICULARES = 14;

	// Tope de horas cátedra semanales por currícula: 40 horas cátedra cubre una jornada semanal
	// completa (turno simple, 8 horas cátedra por día en 5 días hábiles).
	public const int MAXIMO_HORAS_CATEDRA_SEMANALES = 40;

	/// <summary>
	/// Exige que la currícula admita una materia más con la carga horaria indicada.
	/// </summary>
	public static void ExigirAltaPosible(int espaciosOcupados, int horasOcupadas, int horasAIncorporar)
	{
		if (espaciosOcupados + 1 > MAXIMO_ESPACIOS_CURRICULARES)
		{
			throw new LimiteEspaciosCurricularesException();
		}

		ExigirCargaHorariaPosible(horasOcupadas, horasAIncorporar);
	}

	/// <summary>
	/// Exige que la currícula admita la carga horaria indicada. En una modificación,
	/// <paramref name="horasOcupadas"/> debe excluir las horas de la materia que se está cambiando.
	/// </summary>
	public static void ExigirCargaHorariaPosible(int horasOcupadas, int horasAIncorporar)
	{
		if (horasOcupadas + horasAIncorporar > MAXIMO_HORAS_CATEDRA_SEMANALES)
		{
			throw new LimiteHorasSemanalesException();
		}
	}
}
