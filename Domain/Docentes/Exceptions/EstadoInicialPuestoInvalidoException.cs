namespace Domain.Docentes.Exceptions;

public class EstadoInicialPuestoInvalidoException : Exception
{
	private const string ERROR = "Un puesto docente sólo puede darse de alta como pendiente o activo.";


	public EstadoInicialPuestoInvalidoException() : base(ERROR) { }
	public EstadoInicialPuestoInvalidoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public EstadoInicialPuestoInvalidoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
