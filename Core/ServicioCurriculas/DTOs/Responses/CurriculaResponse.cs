namespace Core.ServicioCurriculas.DTOs.Responses;

/// <summary>
/// Una currícula del curso, vigente o cerrada.
/// </summary>
/// <remarks>
/// <see cref="Vigente"/> sale de <c>Curricula.EstaVigente()</c> y no se deduce de que
/// <see cref="FechaFin"/> sea nula: una currícula con fecha de fin futura sigue vigente, y una con
/// fecha de fin pasada no. La pantalla hacía esa deducción por su cuenta y le salía distinto que al
/// dominio, que es el que después rechaza el alta de materias con <c>CurriculaNoVigenteException</c>.
/// </remarks>
public record CurriculaResponse(Guid CursoID, Guid CurriculaID, DateTime FechaInicio, DateTime? FechaFin, bool Vigente);