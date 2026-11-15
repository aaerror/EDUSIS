using Domain.Shared;

namespace Domain.Cursantes;

public interface ICursanteRepository : IRepository<Cursante>
{
	/// <summary>
	/// Hay una inscripción <strong>activa</strong> del alumno en el ciclo lectivo. Mira sólo las
	/// activas a propósito: el traslado de división se modela como cerrar la inscripción actual y
	/// abrir una nueva, así que el mismo alumno tiene más de una inscripción en el mismo ciclo y
	/// contar las cerradas rechazaría el traslado como inscripción duplicada.
	/// </summary>
	Task<bool> ExisteInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo);
	Task<int> ContarCursantesDeDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo);

	/// <summary>
	/// <strong>Todas</strong> las inscripciones de la división: cualquier ciclo lectivo, activas y
	/// cerradas. Es lo contrario de <see cref="ContarCursantesDeDivisionAsync"/>, que cuenta la
	/// matrícula vigente de un ciclo. La consume la guarda de borrado de división: la clave ajena
	/// <c>FK_CURSANTE_DIVISION</c> es <c>Restrict</c>, así que una sola inscripción histórica
	/// alcanza para que el DELETE falle en la base.
	/// </summary>
	Task<int> ContarInscripcionesDeDivisionAsync(Guid divisionID);

	/// <summary>
	/// Las mismas inscripciones que cuenta <see cref="ContarInscripcionesDeDivisionAsync"/>, pero
	/// materializadas: <strong>todas</strong> las de la división, cualquier ciclo lectivo, activas y
	/// cerradas, con sus calificaciones cargadas.
	/// </summary>
	/// <remarks>
	/// La consume el borrado de una división, y la consume dos veces —para contar qué se va a
	/// destruir y para destruirlo—, por el mismo motivo que
	/// <c>IPlanillaAsistenciaRepository.BuscarPorCursantesAsync</c>: dos consultas distintas podrían
	/// dar números distintos y el diálogo informaría una cifra mientras el borrado hace otra.
	/// </remarks>
	Task<IReadOnlyCollection<Cursante>> BuscarInscripcionesDeDivisionAsync(Guid divisionID);
	Task<Cursante?> BuscarInscripcionActivaAsync(Guid alumnoID, CicloLectivo cicloLectivo);

	/// <summary>
	/// Matrícula <strong>vigente</strong> de la división en el ciclo lectivo: sólo las
	/// inscripciones activas. Sin el filtro, un alumno trasladado quedaría listado a la vez en la
	/// división de origen y en la de destino.
	/// </summary>
	Task<IReadOnlyCollection<Cursante>> BuscarPorDivisionAsync(Guid divisionID, CicloLectivo cicloLectivo);
	Task<IReadOnlyCollection<Guid>> BuscarInscriptosEnFechaAsync(Guid divisionID, DateTime fecha);

	/// <summary>
	/// Historial del alumno: <strong>todas</strong> sus inscripciones, de cualquier ciclo lectivo,
	/// activas y cerradas, ordenadas por fecha de inicio.
	/// </summary>
	Task<IReadOnlyCollection<Cursante>> BuscarInscripcionesDelAlumnoAsync(Guid alumnoID);

	/// <summary>
	/// Inscripciones del alumno en un ciclo lectivo, activas <strong>y cerradas</strong>. La
	/// consume el boletín: como <c>Calificacion</c> es entidad hija de <see cref="Cursante"/>, las
	/// notas anteriores a un traslado viven en la inscripción cerrada y hay que unirlas.
	/// </summary>
	Task<IReadOnlyCollection<Cursante>> BuscarInscripcionesDelCicloAsync(Guid alumnoID, CicloLectivo cicloLectivo);

	/// <summary>
	/// Matrícula <strong>vigente</strong> del ciclo lectivo, sin filtrar por división: sólo las
	/// inscripciones activas. Es la base de los reportes institucionales, que recorren la matrícula
	/// completa y agrupan después; sin el filtro, un alumno trasladado se contaría dos veces.
	/// </summary>
	Task<IReadOnlyCollection<Cursante>> BuscarPorCicloLectivoAsync(CicloLectivo cicloLectivo);

	/// <summary>
	/// Ciclos lectivos <strong>habilitados</strong>, del más reciente al más antiguo. No son los
	/// períodos que tienen inscripciones: es el catálogo sobre el que apunta la clave ajena de
	/// <c>Cursante.CicloLectivo.Periodo</c>, así que incluye años sin matrícula todavía.
	/// </summary>
	/// <remarks>
	/// Existe para que la interfaz ofrezca el período en lugar de pedirlo tipeado: un año fuera
	/// del catálogo no se puede elegir, y así no se llega a emitir un documento contra un ciclo
	/// que la base no admite.
	/// </remarks>
	Task<IReadOnlyCollection<CicloLectivo>> ListarCiclosLectivosAsync();
}
