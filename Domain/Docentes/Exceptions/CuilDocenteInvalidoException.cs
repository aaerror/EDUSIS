namespace Domain.Docentes.Exceptions;

public class CuilDocenteInvalidoException : Exception
{
	private const string ERROR = "El CUIL del docente no tiene un formato válido.";


	public CuilDocenteInvalidoException() : base(ERROR) { }
	public CuilDocenteInvalidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public CuilDocenteInvalidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
