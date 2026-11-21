namespace Core.ServicioDivisiones.DTOs.Responses;

/// <summary>
/// En qué está la planilla de asistencia de hoy. Son tres estados y no un <c>bool</c> porque "no
/// cerrada" junta dos situaciones distintas para el preceptor: la que todavía no abrió y la que
/// dejó sin firmar.
/// </summary>
public enum EstadoPlanillaDelDia
{
	SinAbrir,
	Abierta,
	Cerrada
}
