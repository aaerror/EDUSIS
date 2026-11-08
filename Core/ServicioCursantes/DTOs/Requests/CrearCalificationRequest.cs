namespace Core.ServicioCursantes.DTOs.Requests;

/// <param name="CicloLectivo">El año de la cursada: "2026".</param>
/// <param name="Periodo">
/// El tramo del ciclo, con el nombre de un miembro de <c>Domain.Cursantes.Calificaciones.Periodo</c>.
/// </param>
/// <param name="EsRecuperatorio">Si es el segundo intento dentro de ese período.</param>
/// <remarks>
/// El campo del ciclo lectivo se llama <c>CicloLectivo</c> y no <c>Periodo</c> —como en el resto de
/// los requests del módulo— porque desde que las calificaciones distinguen cuatrimestres,
/// recuperaciones y nota final, "período" nombra al tramo y no al año. Dejar los dos significados
/// sobre la misma palabra en el mismo record es una confusión garantizada.
/// </remarks>
public record CrearCalificationRequest(Guid AlumnoID, string CicloLectivo, Guid MateriaID, DateTime Fecha, string Periodo, bool EsRecuperatorio, double Nota, string? Observacion);
