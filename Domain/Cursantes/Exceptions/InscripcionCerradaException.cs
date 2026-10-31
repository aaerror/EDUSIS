namespace Domain.Cursantes.Exceptions;

public class InscripcionCerradaException : Exception
{
	private const string ERROR = "La inscripción ya está cerrada y no admite cambios.";


	public InscripcionCerradaException()
		: base(ERROR) { }
	public InscripcionCerradaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public InscripcionCerradaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
