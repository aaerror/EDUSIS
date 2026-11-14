namespace Domain.Docentes.Exceptions;

public class PuestoDocenteInactivoException : Exception
{
	private const string ERROR = "El puesto docente no se encuentra activo.";


	public PuestoDocenteInactivoException() : base() { }
	public PuestoDocenteInactivoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PuestoDocenteInactivoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}
