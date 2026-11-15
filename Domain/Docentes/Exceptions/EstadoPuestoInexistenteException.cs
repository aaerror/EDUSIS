namespace Domain.Docentes.Exceptions;

public class EstadoPuestoInexistenteException : Exception
{
	private const string ERROR = "El estado de puesto docente indicado no existe.";


	public EstadoPuestoInexistenteException() : base(ERROR) { }
	public EstadoPuestoInexistenteException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public EstadoPuestoInexistenteException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
