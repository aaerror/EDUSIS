namespace Core.ServicioDocumentos.Exceptions;

/// <summary>
/// No hay ninguna planilla del período en la que el alumno figure. <strong>No es "no encontrado"</strong>:
/// el alumno, su inscripción y su división existen; lo que no existe es el respaldo documental.
/// </summary>
/// <remarks>
/// El certificado acredita concurrencia, y emitirlo sobre un período sin asistencia tomada sería una
/// acreditación sin respaldo. El mensaje dice el motivo y no sólo el hecho, porque es la mitad que le
/// sirve a quien lo lee: conserva el texto completo del <c>InvalidOperationException</c> que reemplaza.
/// </remarks>
public class SinAsistenciaRegistradaException : Exception
{
	private const string ERROR = "No hay planillas de asistencia del alumno en el período indicado: no se puede certificar la concurrencia.";


	public SinAsistenciaRegistradaException() : base(ERROR) { }
}
