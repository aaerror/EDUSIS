namespace Core.ServicioDocentes.DTOs.Responses;

/// <summary>
/// Qué destruiría eliminar el legajo del docente, y qué lo impide si algo lo impide. Lo consume el
/// diálogo de confirmación de la pantalla de docentes.
/// </summary>
/// <remarks>
/// <para>
/// A diferencia de los impactos de alumno, división y curso, este <strong>también informa motivos de
/// rechazo</strong> (<see cref="DesignacionesVigentes"/>, <see cref="EsElUltimoAdministrador"/>). Es
/// porque el borrado del docente es el único de los cinco con guardas: cuando se borra un alumno se
/// destruye SU historia, y cuando se borra un docente se destruye la de OTROS.
/// </para>
/// <para>
/// No lleva cuenta de planillas de asistencia firmadas. Las planillas conservan el
/// <c>PreceptorID</c> del docente borrado como firma histórica —es la consecuencia de haber quitado
/// <c>FK_PLANILLA_ASISTENCIA_DOCENTE</c>— y eso es un hecho fijo de la operación, no una cantidad:
/// contarlo exigiría recorrer <c>planilla_asistencia</c> entera, porque esa columna se quedó sin
/// índice en la misma migración.
/// </para>
/// </remarks>
public record ImpactoEliminacionDocenteResponse(
	int DesignacionesVigentes,
	int DesignacionesCerradas,
	int DivisionesQueQuedanSinPreceptor,
	int Licencias,
	bool TieneUsuario,
	bool EsElUltimoAdministrador)
{
	/// <summary>
	/// El borrado va a ser rechazado por <c>Core</c>. La pantalla lo usa para explicar qué hay que
	/// desarmar antes, en lugar de ofrecer una confirmación condenada a fallar.
	/// </summary>
	public bool EstaBloqueado =>
		DesignacionesVigentes > 0 || EsElUltimoAdministrador;

	/// <summary>
	/// El borrado destruye algo más que el legajo. En <c>false</c> el diálogo se resuelve en una
	/// línea, sin enumerar ceros.
	/// </summary>
	public bool TieneVinculos =>
		DesignacionesCerradas > 0 || DivisionesQueQuedanSinPreceptor > 0 || Licencias > 0 || TieneUsuario;
}
