namespace Domain.Docentes.Exceptions;

public class PosicionPuestoRequeridaException : Exception
{
	private const string ERROR = "Debe ingresar la posición del cargo docente.";


	public PosicionPuestoRequeridaException() : base(ERROR) { }
	public PosicionPuestoRequeridaException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PosicionPuestoRequeridaException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
