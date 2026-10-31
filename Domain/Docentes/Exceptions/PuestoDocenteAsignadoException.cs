namespace Domain.Docentes.Exceptions;

public class PuestoDocenteAsignadoException : Exception
{
	private const string ERROR = "El puesto docente ya se encuentra activo y asignado al docente.";


	public PuestoDocenteAsignadoException() : base() { }
	public PuestoDocenteAsignadoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PuestoDocenteAsignadoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}