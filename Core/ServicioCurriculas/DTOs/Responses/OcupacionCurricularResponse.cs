namespace Core.ServicioCurriculas.DTOs.Responses;

/// <summary>
/// Cuánto de la currícula está ocupado y cuánto queda, en espacios curriculares y en horas cátedra
/// semanales.
/// </summary>
/// <remarks>
/// <para>
/// Es el presupuesto de la currícula. <c>TopesCurriculares</c> ya imponía los dos máximos y
/// <c>ServicioMateria</c> ya calculaba los dos consumos —justo antes de rechazar un alta— pero los
/// descartaba: el número que el usuario necesitaba para no chocarse con el tope se computaba un
/// milisegundo antes de tirarle la excepción encima. Acá se lo devuelve.
/// </para>
/// <para>
/// Los máximos viajan en el response en lugar de que la pantalla los lea de <c>TopesCurriculares</c>:
/// son una regla del dominio y la vista no tiene por qué conocerla, le alcanza con saber que de 14
/// van 9. Si algún día el tope depende del nivel educativo, cambia la consulta y no la pantalla.
/// </para>
/// </remarks>
public record OcupacionCurricularResponse(
	int EspaciosOcupados,
	int EspaciosMaximos,
	int HorasOcupadas,
	int HorasMaximas)
{
	/// <summary>Espacios curriculares que todavía admite. Nunca negativo.</summary>
	public int EspaciosDisponibles => Math.Max(0, EspaciosMaximos - EspaciosOcupados);

	/// <summary>Horas cátedra semanales que todavía admite. Nunca negativo.</summary>
	public int HorasDisponibles => Math.Max(0, HorasMaximas - HorasOcupadas);

	/// <summary>
	/// Entra al menos una materia más de una hora. Es la precondición exacta de
	/// <c>TopesCurriculares.ExigirAltaPosible</c> con la carga mínima.
	/// </summary>
	public bool AdmiteOtraMateria => EspaciosDisponibles > 0 && HorasDisponibles > 0;
}
