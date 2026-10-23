namespace Core.ServicioDocumentos.DTOs.Requests;

/// <summary>
/// Parámetros del certificado de asistencia: el período que se acredita se elige al emitirlo y
/// no se deduce del ciclo lectivo.
/// </summary>
public record EmitirCertificadoAsistenciaRequest(
	Guid AlumnoID,
	string Periodo,
	DateTime Desde,
	DateTime Hasta,
	string? Finalidad);
