namespace Domain.Cursantes.Exceptions;

internal class SinDatosAlumnoException : Exception
{
	private const string ERROR = "Alumno sin especificar.";


	public SinDatosAlumnoException()
		: base(ERROR) { }
	public SinDatosAlumnoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public SinDatosAlumnoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
