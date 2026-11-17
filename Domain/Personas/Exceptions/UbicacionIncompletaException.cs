namespace Domain.Personas.Exceptions;

public class UbicacionIncompletaException : Exception
{
	private const string ERROR = "La ubicación posee datos incompletos.";


	public UbicacionIncompletaException()
		: base(ERROR) { }

	public UbicacionIncompletaException(string parametro)
		: base(string.Format($"{ERROR} Parámetro: {parametro}")) { }

	public UbicacionIncompletaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR} Parámetro: {parametro}"), exception) { }
}
