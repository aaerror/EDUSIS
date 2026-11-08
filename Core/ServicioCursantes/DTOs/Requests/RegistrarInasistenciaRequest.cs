namespace Core.ServicioCursantes.DTOs.Requests;

/// <param name="CicloLectivo">El año de la cursada: "2026".</param>
/// <param name="Periodo">
/// El tramo del ciclo, con el nombre de un miembro de <c>Domain.Cursantes.Calificaciones.Periodo</c>.
/// </param>
/// <param name="EsRecuperatorio">Si la inasistencia es a un recuperatorio de ese período.</param>
public record RegistrarInasistenciaRequest(Guid AlumnoID, string CicloLectivo, Guid MateriaID, DateTime Fecha, string Periodo, bool EsRecuperatorio, string? Observacion);
