namespace Core.ServicioReportes.Exceptions;

/// <summary>
/// El alcance pedido para el reporte —un ciclo lectivo, con o sin división— no tiene ninguna
/// inscripción. <strong>No es "no encontrado"</strong>: el ciclo y la división existen y están vacíos.
/// </summary>
/// <remarks>
/// El constructor con parámetro nombra el ciclo lectivo, que es el dato que el mensaje anterior
/// interpolaba y lo que le dice al usuario sobre qué período preguntó.
/// </remarks>
public class SinInscripcionesEnElAlcanceException : Exception
{
	private const string ERROR = "No hay inscripciones registradas para el alcance solicitado";


	public SinInscripcionesEnElAlcanceException()
		: base($"{ ERROR }.") { }

	public SinInscripcionesEnElAlcanceException(string cicloLectivo)
		: base($"{ ERROR } en el ciclo lectivo { cicloLectivo }.") { }
}
