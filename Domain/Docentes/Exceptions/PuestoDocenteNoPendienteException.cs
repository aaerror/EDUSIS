namespace Domain.Docentes.Exceptions;

public class PuestoDocenteNoPendienteException : Exception
{
	private const string ERROR = "El puesto docente no se encuentra pendiente.";


	public PuestoDocenteNoPendienteException() : base(ERROR) { }
	public PuestoDocenteNoPendienteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PuestoDocenteNoPendienteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
