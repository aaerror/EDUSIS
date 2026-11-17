namespace Domain.Catedras.Exceptions;

public class SinDatosDocenteException : Exception
{
	private const string ERROR = "Se deben especificar los datos del docente.";


	public SinDatosDocenteException()
		: base(ERROR) { }

	public SinDatosDocenteException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public SinDatosDocenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
