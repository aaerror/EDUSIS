namespace Domain.Docentes.Exceptions;

internal class PuestoDocenteNoEncontradoException : Exception
{
	private const string ERROR = "El puesto docente no se encuentra asignado al docente.";


	public PuestoDocenteNoEncontradoException() : base() { }
	public PuestoDocenteNoEncontradoException(string parametro)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}")) { }
	public PuestoDocenteNoEncontradoException(string parametro, Exception exception)
		: base(string.Format($"{ERROR}. Parámetro: {parametro}"), exception) { }
}