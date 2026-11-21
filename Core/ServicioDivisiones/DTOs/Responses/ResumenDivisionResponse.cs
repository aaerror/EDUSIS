namespace Core.ServicioDivisiones.DTOs.Responses;

/// <summary>
/// Cómo está la división en el ciclo lectivo: su matrícula, si puede funcionar hoy y cómo le va en
/// asistencia y en notas. Lo consume la ficha de la división elegida.
/// </summary>
/// <remarks>
/// <para>
/// <strong>LOS CAMPOS DE "HOY" SON NULOS FUERA DEL AÑO EN CURSO.</strong>
/// <see cref="PlanillaDeHoy"/>, <see cref="PreceptorDeLicencia"/> y los de cátedras describen el
/// presente y no un ciclo: la planilla de hoy de 2024 no existe, y ni la cátedra ni la licencia
/// tienen ciclo lectivo, así que informarlas para un año cerrado mostraría el estado de hoy con la
/// etiqueta de otro año.
/// </para>
/// <para>
/// El promedio y la asistencia miran sólo lo registrado <strong>en esta división</strong>. A un
/// alumno que llegó por traslado no le cuentan las notas de la división de origen, que viven en su
/// inscripción cerrada de allá; el perfil del alumno sí las une, porque habla del alumno y no de
/// la división.
/// </para>
/// </remarks>
public record ResumenDivisionResponse(
	string CicloLectivo,
	bool EsCicloEnCurso,

	// Matrícula
	int Inscriptos,
	int Cupo,
	int Recursantes,
	int Bajas,

	// Estado de hoy: nulos fuera del año en curso
	EstadoPlanillaDelDia? PlanillaDeHoy,
	bool? PreceptorDeLicencia,
	int? CatedrasSinDocente,
	int? CatedrasConSuplente,
	int? HorasSinAsignar,
	IReadOnlyCollection<string>? MateriasSinCatedra,

	// Asistencia del ciclo
	int Planillas,
	int PlanillasSinCerrar,
	decimal? PorcentajeAsistencia,
	int Faltas,
	int Tardanzas,
	IReadOnlyCollection<AlumnoConFaltasResponse> AlumnosConMasFaltas,

	// Rendimiento del ciclo
	decimal? Promedio,
	int AlumnosConMateriasDesaprobadas,
	int AusenciasAExamen);
